using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace DataCalc
{
    public static class Calc
    {
        #region Константы

        /// <summary>
        /// Радиус земли
        /// </summary>
        const double R = 6371000;

        #endregion
        
        public static TrassalOutParam Trassal(TrassalInParam param)
        {
            var e1 = ToGeocetnricCoord(param.Start);
            var e2 = ToGeocetnricCoord(param.End);
            // Перевод координат начальной и конечной точек трассы в геоцентрическую систему.
            var r1 = e1 * (R + param.h);
            var r2 = e2 * (R + param.h);
            
            // Угловая величина дуги большого круга, соединяющей начальную и конечную точки трассы.
            var delta = Arccos(e1 * e2);
            
            // Угловая величина дуги большого круга, пройденной за время t.
            var alpha = ((param.V * param.t) / (R + param.h)) * (180 / Math.PI);

            // Вектор координат ЛА в момент t.
            var p = r1 * (Sin(delta - alpha) / Sin(delta)) + r2 * (Sin(alpha) / Sin(delta));
            
            // Нормированный вектор
            var e = e1 * (Sin(delta - alpha) / Sin(delta)) + e2 * (Sin(alpha) / Sin(delta));
            
            // Географические координаты ЛА в момент t.
            var f = Arcsin(e.Z);
            var lambda = Arccos(e.X / Cos(f));
            if (e.Y < 0) lambda *= -1;
            else if (e.Y == 0) lambda *= 0;

            // Проверка на выход из диапазона
            if 
            (
                Math.Min(param.Start.Fi, param.End.Fi) > Math.Round(f, 6)
                || Math.Round(f, 6) > Math.Max(param.Start.Fi, param.End.Fi)
                || Math.Min(param.Start.Lambda, param.End.Lambda) > Math.Round(lambda, 6) 
                || Math.Round(lambda, 6) > Math.Max(param.Start.Lambda, param.End.Lambda)
            )
            {
                return null;
            }

            // Вектор скорости ЛА в момент t
            var p_speed = 
                (r2 * (Cos(alpha) / Sin(delta))
                 - r1 * (Cos(delta - alpha) / Sin(delta)))
                * (param.V / (R + param.h));
            
            // Единичный вектор направления строительной оси ЛА в момент t^
            var v = p_speed / GeocentrCoord.Abs(p_speed);
            
            // Координаты единичного вектора, направленного вдоль меридиана:
            var m = new GeocentrCoord
            {
                X = -Sin(f)*Cos(lambda),
                Y = -Sin(f)*Sin(lambda),
                Z = Cos(f)
            };
            
            // Курсовой угол:
            var psi = Arccos(v * m);
            var check = param.End.Lambda - param.Start.Lambda;
            if (0 < check && check < 180 || check < -180)
                psi = Math.Abs(psi);
            else
                psi *= psi > 0 ? -1 : 1;

            return new TrassalOutParam()
            {
                CurrentGeographCoord = new GeographCoord(){Fi = f, Lambda = lambda},
                Psi = psi,
                p = new GeocentrCoord(){X = p.X / 1000, Y = p.Y / 1000, Z = p.Z / 1000},
                v = v
            };
        }

        /// <summary>
        /// Формирование данных о пролете ЛА по ломанной
        /// </summary>
        /// <param name="height">Высота полета</param>
        /// <param name="speed">Скорость полета</param>
        /// <param name="time">Шаг времени</param>
        /// <param name="coords">Координаты ломанной</param>
        /// <param name="time_accuracy">Точность по времени</param>
        /// <returns>Данные о пролете по ломанной</returns>
        public static ObservableCollection<Param> MakeTrassa(double height, double speed, double time, List<GeographCoord> coords, double time_accuracy = 0.001)
        {
            var in_param = new TrassalInParam() {h = height, V = speed};
            var pl = new List<Param>();
            for (var i = 1; i < coords.Count; i++)
            {
                TrassalOutParam res;
                var tm = time;
                
                in_param.t = 0;
                in_param.Start = coords[i - 1];
                in_param.End = coords[i];
                
                while (true)
                {
                    if ((res = Trassal(in_param)) is not null)
                    {
                        if (tm <= time_accuracy)
                            break;
                        in_param.t += tm;
                    }
                    else
                    {
                        in_param.t -= tm;
                        tm /= 2;
                        in_param.t += tm;
                    }
                }
                pl.Add(new Param()
                {
                    Fi = res.CurrentGeographCoord.Fi, 
                    Lambda = res.CurrentGeographCoord.Lambda,
                    Height = height,
                    Psi = res.Psi,
                    Time = in_param.t,
                    Kren = 0,
                    Tangaz = 0,
                    Start = new GeographCoord(){Fi = coords[i-1].Fi, Lambda = coords[i-1].Lambda},
                    End = new GeographCoord(){Fi = coords[i].Fi, Lambda = coords[i].Lambda}
                });
            }

            StreamWriter f = new StreamWriter("data.txt");
            foreach (var p in pl)
            {
                var tm = 0.0;
                while (true)
                {
                    var res = Trassal(new (){h = p.Height, t = tm, Start = p.Start, End = p.End, V = speed});
                    Console.WriteLine(new Param()
                    {
                        Time = tm, Fi = res.CurrentGeographCoord.Fi, Lambda = res.CurrentGeographCoord.Lambda,
                        Height = height, Psi = res.Psi, Tangaz = 0, Kren = 0,
                    });
                    tm += time;
                    if (!(tm > p.Time)) continue;
                    if (p == pl.Last())
                    {
                        res = Trassal(new() {h = p.Height, t = p.Time, Start = p.Start, End = p.End, V = speed});
                        Console.WriteLine(
                            new Param()
                            {
                                Time = p.Time, Fi = res.CurrentGeographCoord.Fi, Lambda = res.CurrentGeographCoord.Lambda,
                                Height = height, Psi = res.Psi, Tangaz = 0, Kren = 0,
                            });
                    }
                    break;
                }
            }
            f.Close();
            
            return new ObservableCollection<Param>(pl);
        }

        /// <summary>
        /// Расчет нормального распределения, принимает mu, sigma
        /// </summary>
        public static dynamic RandomNorm { get; }
        static Calc()
        {
            ScriptEngine engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();
            engine.ExecuteFile("rnd.py", scope);
            RandomNorm = scope.GetVariable("rand_norm");
        }

        #region Приватные функции

        /// <summary>
        /// Создание трехмерного вектора
        /// </summary>
        /// <param name="coord">Географические координты точки</param>
        /// <returns>Возвращает трехмерный вектор</returns>
        private static GeocentrCoord ToGeocetnricCoord(GeographCoord coord) =>
            new GeocentrCoord()
            {
                X = Cos(coord.Fi) * Cos(coord.Lambda),
                Y = Cos(coord.Fi) * Sin(coord.Lambda),
                Z = Sin(coord.Fi)
            };

        /// <summary>
        /// Возвращает sin
        /// </summary>
        /// <param name="degr">Градусы</param>
        /// <returns>Sin</returns>
        private static double Sin(double degr) =>
            Math.Sin(ToRadian(degr));
        
        /// <summary>
        /// Возвращает сos
        /// </summary>
        /// <param name="degr">Градусы</param>
        /// <returns>Cos в грудсах</returns>
        private static double Cos(double degr) =>
            Math.Cos(ToRadian(degr));
        
        /// <summary>
        /// Возвращает Arcsin
        /// </summary>
        /// <param name="nm">Число</param>
        /// <returns>Arcsin</returns>
        private static double Arcsin(double nm) =>
            ToDegrees(Math.Asin(nm));
        
        /// <summary>
        /// Возвращает Arccos
        /// </summary>
        /// <param name="nm">Число</param>
        /// <returns>Arccos</returns>
        private static double Arccos(double nm) =>
            ToDegrees(Math.Acos(nm));
        

        /// <summary>
        /// Переводит градусы в радианы
        /// </summary>
        /// <param name="degrees">Градусы</param>
        /// <returns>Радианы</returns>
        private static double ToRadian(double degrees) =>
            (degrees * Math.PI) / 180;

        
        /// <summary>
        /// Переводит радианы в градусы
        /// </summary>
        /// <param name="rad">Радианы</param>
        /// <returns>Градусы</returns>
        private static double ToDegrees(double rad) =>
            (rad * 180) / Math.PI;

        #endregion
    }
}