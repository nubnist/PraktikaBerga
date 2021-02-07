using System;

namespace DataCalc
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
             /// Перегрузка оператора для сложения двух трехмерных векоров
             /// </summary>
             /// <param name="op1">Первый вектор</param>
             /// <param name="op2">Второй вектор</param>
             /// <returns>Новый вектор являющийся суммой двух других</returns>
             public static GeocentrCoord operator +(GeocentrCoord op1, GeocentrCoord op2)
                 =>
                     new GeocentrCoord()
                     {
                         X = op1.X + op2.X,
                         Y = op1.Y + op2.Y,
                         Z = op1.Z + op2.Z,
                     };
             
             /// <summary>
             /// Перегрузка оператора для вычитания двух трехмерных векоров
             /// </summary>
             /// <param name="op1">Первый вектор</param>
             /// <param name="op2">Второй вектор</param>
             /// <returns>Новый вектор являющийся разностью двух других</returns>
             public static GeocentrCoord operator -(GeocentrCoord op1, GeocentrCoord op2)
                 =>
                     new GeocentrCoord()
                     {
                         X = op1.X - op2.X,
                         Y = op1.Y - op2.Y,
                         Z = op1.Z - op2.Z,
                     };

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
             /// Перегрузка оператора деления вектора на число
             /// </summary>
             /// <param name="op1">Вектор</param>
             /// <param name="op2">Число</param>
             /// <returns>Вектор деленный начисло</returns>
             public static GeocentrCoord operator /(GeocentrCoord op1, double op2)
                 => new GeocentrCoord()
                 {
                     X = op1.X / op2,
                     Y = op1.Y / op2,
                     Z = op1.Z / op2,
                 };

             /// <summary>
             /// Нахождение модуля вектора.
             /// </summary>
             /// <param name="op1">Трехмерный вектор</param>
             /// <returns>Модуль вектора</returns>
             public static double Abs(GeocentrCoord op1)
                 => Math.Sqrt(op1.X * op1.X + op1.Y * op1.Y + op1.Z * op1.Z);

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

}