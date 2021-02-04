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
             

             /// <summary>
             /// Перегрузка оператора умножение вектора на число
             /// </summary>
             /// <param name="op1">Вектор</param>
             /// <param name="op2">Число</param>
             /// <returns>Вектор умноженный начисло</returns>
             public static GeocentrCoord operator *(GeocentrCoord op1, double op2)
                 => new GeocentrCoord()
                 {
                     X = op1.X * op2,
                     Y = op1.Y * op2,
                     Z = op1.Z * op2,
                 };

             /// <summary>
             /// Перегрузка оператора умножения для двух векторов - возвращает скалярное произведение векторов в пространстве.
             /// </summary>
             /// <param name="op1">Первый вектор</param>
             /// <param name="op2">Второй вектор</param>
             /// <returns></returns>
             public static double operator *(GeocentrCoord op1, GeocentrCoord op2)
                 => op1.X * op2.X + op1.Y * op2.Y + op1.Z * op2.Z;

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
            var e1 = ToGeocetnricCoord(param.StartGeographCoord);
            var e2 = ToGeocetnricCoord(param.EndGeographCoord);
            var r1 = e1 * (R + param.h);
            var r2 = e2 * (R + param.h);
            
            // Угловая величина дуги большого круга, соединяющей начальную и конечную точки трассы.
            var delta = ToDegrees(Math.Acos(e1 * e2));
            
            //Угловая величина дуги большого круга, пройденной за время t.
            var alpha = ((param.V * param.t) / R + param.h) * (180 / Math.PI);

            return new TrassalOutParam();
        }

        #region Приватные функции

        /// <summary>
        /// Создание трехмерного вектора
        /// </summary>
        /// <param name="coord">Географические координты точки</param>
        /// <param name="h">Высота полета</param>
        /// <returns>Возвращает трехмерный вектор</returns>
        private static GeocentrCoord ToGeocetnricCoord(GeographCoord coord) =>
            new GeocentrCoord()
            {
                X = Math.Cos(ToRad(coord.Fi)) * Math.Cos(ToRad(coord.Lambda)),
                Y = Math.Cos(ToRad(coord.Fi)) * Math.Sin(ToRad(coord.Lambda)),
                Z = Math.Sin(ToRad(coord.Fi))
            };

        /// <summary>
        /// Переводит градусы в радианы
        /// </summary>
        /// <param name="degrees">Градусы</param>
        /// <returns>Радианы</returns>
        private static double ToRad(double degrees) =>
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