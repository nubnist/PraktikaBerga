using System;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace DataCalc
{
    public partial class Calc
    {
        #region Константы

        /// <summary>
        /// Радиус земли
        /// </summary>
        const double R = 6371000;

        #endregion
        
        #region Приватные функции
        
        /// <summary>
        /// Расчет нормального распределения, принимает mu, sigma
        /// </summary>
        private static dynamic RandomNorm { get; }
        static Calc()
        {
            ScriptEngine engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();
            engine.ExecuteFile("rnd.py", scope);
            RandomNorm = scope.GetVariable("rand_norm");
        }

        private static (double, double) CoordRand(double fi, double lambda, double sigma)
        {
            double e = RandomNorm(0, sigma);
            var rand = new Random();
            var n = rand.Next(0, Int32.MaxValue);
            var a = 2 * Math.PI * (n / Int32.MaxValue);
            var f_r = fi + e * Cos(a) / R * (180 / Math.PI);
            var l_r = lambda + e * Cos(a) / (R * Cos(fi)) * (180 / Math.PI);

            return (f_r, l_r);
        }

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