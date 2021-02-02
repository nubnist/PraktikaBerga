using System;

namespace DataCalc
{
    public class Calc
    {
        #region Структуры данных для измерений

         /// <summary>
        /// Структура сохраняет в себе широту и долготу точки трассы
        /// </summary>
        public struct GeographicCoordinate
        {
            public double Fi;
            public double Lambda;
        }
        
        /// <summary>
        /// Входные параметры для функции Trassal
        /// </summary>
        public struct TrassalInputParameters
        {
            /// <summary>
            /// Широта и долгота начальной точки трассы (градусы)
            /// </summary>
            public GeographicCoordinate StartGeographicCoordinate { get; set; }
            /// <summary>
            /// Широта и долгота конечно точки трассы (градусы)
            /// </summary>
            public GeographicCoordinate EndGeographicCoordinate { get; set; }
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
        /// Геоцентрические координаты
        /// </summary>
        public struct GeocentricCoordinate
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }
        
        /// <summary>
        /// Выходные параметры для функции Trassal
        /// </summary>
        public struct TrassalOutputParameters
        {
            /// <summary>
            /// Широта и долгота нахождения ЛА в момент t;
            /// </summary>
            public GeographicCoordinate CurrentGeographicCoordinate { get; set; }
            /// <summary>
            /// Истинный курсовой угол ЛА в момент t;
            /// </summary>
            public double Psi { get; set; }
            /// <summary>
            /// Координаты точки нахождения ЛА в момент t в геоцентрической системе.
            /// </summary>
            public GeocentricCoordinate p { get; set; }
            /// <summary>
            /// Координаты единичного вектора, направленного вдоль строительной оси ЛА,
            /// в момент t в геоцентрической системе.
            /// </summary>
            public GeocentricCoordinate v { get; set; }
        }

        #endregion

        public TrassalOutputParameters Trassal(TrassalInputParameters param)
        {
            

            return new TrassalOutputParameters();
        }
    }
}