using System;

namespace DataCalc
{
    public class Calc
    {
        #region Константы

        /// <summary>
        /// Радиус земли
        /// </summary>
        const double R = 6371;

        #endregion
        
        public TrassalOutParam Trassal(TrassalInParam param)
        {
            var e1 = ToGeocetnricCoord(param.StartGeographCoord);
            var e2 = ToGeocetnricCoord(param.EndGeographCoord);
            // Перевод координат начальной и конечной точек трассы в геоцентрическую систему.
            var r1 = e1 * (R + param.h);
            var r2 = e2 * (R + param.h);
            
            // Угловая величина дуги большого круга, соединяющей начальную и конечную точки трассы.
            var delta = Arccos(e1 * e2);
            
            // Угловая величина дуги большого круга, пройденной за время t.
            var alpha = ((param.V * param.t) / (R + param.h)) * (180 / Math.PI);

            #region Используется в следующих строках для расчета векторов.
            Func<GeocentrCoord, GeocentrCoord> npr = obj => obj * (Sin(delta - alpha) / Sin(alpha)) + 
                                                            obj * (Sin(alpha) / Sin(delta));
            #endregion
            
            // Вектор координат ЛА в момент t.
            var p = npr(r1) + npr(r2);
            
            // Нормированный вектор
            var e = npr(e1) + npr(e2);
            
            // Географические координаты ЛА в момент t.
            var f = Math.Asin(e.Z);
            var lambda = Arccos(e.X / Cos(f));
            f = ToDegrees(f);
            if (e.Y < 0) lambda *= -1;
            else if (e.Y == 0) lambda *= 0;
            
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
            
            

            return new TrassalOutParam()
            {
                CurrentGeographCoord = new GeographCoord(){Fi = f, Lambda = lambda},
                Psi = psi,
                p = p,
                v = v
            };
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
                X = Math.Cos(ToRadian(coord.Fi)) * Math.Cos(ToRadian(coord.Lambda)),
                Y = Math.Cos(ToRadian(coord.Fi)) * Math.Sin(ToRadian(coord.Lambda)),
                Z = Math.Sin(ToRadian(coord.Fi))
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
        private double Arcsin(double nm) =>
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