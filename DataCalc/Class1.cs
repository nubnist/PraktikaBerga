using System;

namespace DataCalc
{
    public class Calc
    {
        #region Структуры данных для измерений

         /// <summary>
        /// Структура сохраняет в себе широту и долготу точки трассы
        /// </summary>
        public struct GeographCoord
        {
            /// <summary>
            /// Широта (градусы)
            /// </summary>
            public double Fi;
            /// <summary>
            /// Долгота (градусы)
            /// </summary>
            public double Lambda;
        }
         
         /// <summary>
         /// Геоцентрические координаты
         /// </summary>
         public struct GeocentrCoord
         {
             public double X { get; set; }
             public double Y { get; set; }
             public double Z { get; set; }
         }
        
        /// <summary>
        /// Входные параметры для функции Trassal
        /// </summary>
        public struct TrassalInParam
        {
            /// <summary>
            /// Широта и долгота начальной точки трассы (градусы)
            /// </summary>
            public GeographCoord StartGeographCoord { get; set; }
            /// <summary>
            /// Широта и долгота конечно точки трассы (градусы)
            /// </summary>
            public GeographCoord EndGeographCoord { get; set; }
            /// <summary>
            /// Высота полета (метры)
            /// </summary>
            public double h { get; set; }
            /// <summary>
            /// Скорость полета (метры в секунду)
            /// </summary>
            public double V { get; set; }
            /// <summary>
            /// Время, прошедшее с начала полета ЛА по трассе (секунды)
            /// </summary>
            public double t { get; set; }

        }

        /// <summary>
        /// Выходные параметры для функции Trassal
        /// </summary>
        public struct TrassalOutParam
        {
            /// <summary>
            /// Широта и долгота нахождения ЛА в момент t;
            /// </summary>
            public GeographCoord CurrentGeographCoord { get; set; }
            /// <summary>
            /// Истинный курсовой угол ЛА в момент t;
            /// </summary>
            public double Psi { get; set; }
            /// <summary>
            /// Координаты точки нахождения ЛА в момент t в геоцентрической системе.
            /// </summary>
            public GeocentrCoord p { get; set; }
            /// <summary>
            /// Координаты единичного вектора, направленного вдоль строительной оси ЛА,
            /// в момент t в геоцентрической системе.
            /// </summary>
            public GeocentrCoord v { get; set; }
        }

        #endregion

        #region Константы

        /// <summary>
        /// Радиус земли
        /// </summary>
        const double R = 6371;

        #endregion
        
        public TrassalOutParam Trassal(TrassalInParam param)
        {
            // Перевод координат начальной и конечной точек трассы в геоцентрическую систему.
            GeocentrCoord r1 = ToGeocetnricCoord(param.StartGeographCoord, param.h);
            GeocentrCoord r2 = ToGeocetnricCoord(param.EndGeographCoord, param.h);
            
            
            

            return new TrassalOutParam();
        }

        #region Приватные функции

        /// <summary>
        /// Перевод точек трассы в геоцентрическую систему
        /// </summary>
        /// <param name="coord">Географические координты точки</param>
        /// <param name="h">Высота полета</param>
        /// <returns>Возвращает координаты точки в геоцентрической системе</returns>
        private static GeocentrCoord ToGeocetnricCoord(GeographCoord coord, double h) =>
            new GeocentrCoord()
            {
                X = Math.Cos(ToDegrees(coord.Fi)) * Math.Cos(ToDegrees(coord.Lambda)) * (R + h),
                Y = Math.Cos(ToDegrees(coord.Fi)) * Math.Sin(ToDegrees(coord.Lambda)) * (R + h),
                Z = Math.Sin(ToDegrees(coord.Fi)) * (R + h)
            };

        /// <summary>
        /// Переводит градусы в радианы
        /// </summary>
        /// <param name="radian">Градусы</param>
        /// <returns>Радианы</returns>
        private static double ToDegrees(double radian) =>
            (radian * Math.PI) / 180;

        #endregion
        }
}