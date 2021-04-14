using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace DataCalc
{

    public class Package
    {
        /// <summary>
        /// Время
        /// </summary>
        public double Time { get; set; }
        /// <summary>
        /// Несущая частота
        /// </summary>
        public double F { get; set; }
        /// <summary>
        /// Длительность сигнала
        /// </summary>
        public double Tau { get; set; }
        /// <summary>
        /// Косинус угла пелленга
        /// </summary>
        public double С { get; set; }
        /// <summary>
        /// Борт
        /// </summary>
        public CharacteristicRAN.Boards Board { get; set; }
        /// <summary>
        /// Код типа РЭС
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Условный номер типа
        /// </summary>
        public int NType { get; set; }
    }

#region Структуры данных для процедуры MakeStream

 /// <summary>
/// Параметры системы РЭН, размещенной на борту ЛА
/// </summary>
public class CharacteristicRAN
{
    /// <summary>
    /// Борты ЛА
    /// </summary>
    public enum Boards
    {
        L,
        R
    }
    
    /// <summary>
    /// Борт
    /// </summary>
    public Boards Board { get; set; }
    /// <summary>
    /// Продолжительность подцикла
    /// </summary>
    public double Duration { get; set; }
    /// <summary>
    /// Минимальная принимаемая частота сигналов
    /// </summary>
    public double MinSignal { get; set; }
    /// <summary>
    /// Максимальная принимаемая частота сигналов
    /// </summary>
    public double MaxSignal { get; set; }

    /// <summary>
    /// Минимальный угол пеленга
    /// </summary>
    public double BMin { get; set; }
    /// <summary>
    /// Максимальный угол пелена
    /// </summary>
    public double BMax { get; set; }
    /// <summary>
    /// Количество импульсов, объединяемыъ в од ин пакет при приеме
    /// серии близких по характеристикам сигналов
    /// </summary>
    public int N { get; set; }
}
 
/// <summary>
/// Характеристики процесса перемещения летательного аппарата (ЛА)
/// </summary>
public class CharacteristicMovingLA
{
    public double Height { get; set; }
    public double Speed { get; set; }
    public double Time { get; set; }
    public List<GeographCoord> Coords { get; set; }
}

/// <summary>
/// Характеристики источника ИРИ
/// </summary>
public class CharacteristicIRI
{
    /// <summary>
    /// Условный номер ИЛИ 
    /// </summary>
    public int NType { get; set; }
    /// <summary>
    /// Географические координаты точки размещения ИРИ на поверхности земли
    /// </summary>
    public GeographCoord Coord { get; set; }
}

/// <summary>
/// Характеристики излучаемого потока сигналов
/// </summary>
public class CharacteristicStream
{
    /// <summary>
    /// Номер типорежима
    /// </summary>
    public int Nres { get; set; }
    /// <summary>
    /// Несущая частота
    /// </summary>
    public double F { get; set; }
    /// <summary>
    /// Длительность импульса
    /// </summary>
    public double Tau { get; set; }
    /// <summary>
    /// Межимпульсный интервал
    /// </summary>
    public double Dt { get; set; }
    /// <summary>
    /// Продолжительность подцикла
    /// </summary>
    public double Duration { get; set; }
}

/// <summary>
/// База каталогов типов РЭС
/// </summary>
public class Catalog
{
    /// <summary>
    /// Код типа РЭС
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Условный номер типа
    /// </summary>
    public int NType { get; set; }
    /// <summary>
    /// Номер типорежима
    /// </summary>
    public int Nres { get; set; }
    /// <summary>
    /// Минимальная несущая частота сигналов
    /// </summary>
    public double FMin { get; set; }
    /// <summary>
    /// Максимальная несущая частота сигналов
    /// </summary>
    public double FMax { get; set; }
    /// <summary>
    /// Минимальная длительность импульса
    /// </summary>
    public double TauMin { get; set; }
    /// <summary>
    /// Максимальная длительность импульса
    /// </summary>
    public double TauMax { get; set; }
    /// <summary>
    /// Минимальный межимпульсный интервал
    /// </summary>
    public double DtMin { get; set; }
    /// <summary>
    /// Максимальный межимпульсный интервал
    /// </summary>
    public double DtMax { get; set; }
    /// <summary>
    /// Код вида модуляции
    /// </summary>
    public string Kod { get; set; }
}

#endregion
   
    
    
    #region Структуры данных для измерений
    
    /// <summary>
    /// Содержит данные точки трассы
    /// </summary>
    public class Param
    {
        public double Time { get; set; }
        public double Fi { get; set; }
        public double Lambda { get; set; }
        public double Height { get; set; }
        public double Psi { get; set; }
        public double Tangaz { get; set; }
        public double Kren { get; set; }
        public GeographCoord Start { get; set; }
        public GeographCoord End { get; set; }
        
        public GeocentrCoord v { get; set; }
        public GeocentrCoord p { get; set; }

        public override string ToString()
        {
            return $"{Time:0.000000000}\t{Fi:0.000000}\t{Lambda:0.000000}\t{Height:0.000}\t{Psi:0.000000}\t{Tangaz}\t{Kren}";
        }
    }

    /// <summary>
    /// Структура сохраняет в себе широту и долготу точки трассы
    /// </summary>
    public class GeographCoord
    {
        /// <summary>
        /// Широта (градусы)
        /// </summary>
        public double Fi { get; set; }
        /// <summary>
        /// Долгота (градусы)
        /// </summary>
        public double Lambda{ get; set; }
    }
     
     /// <summary>
     /// Геоцентрические координаты
     /// </summary>
     public class GeocentrCoord
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
    public class TrassalInParam
    {
        /// <summary>
        /// Широта и долгота начальной точки трассы (градусы)
        /// </summary>
        public GeographCoord Start { get; set; }
        /// <summary>
        /// Широта и долгота конечно точки трассы (градусы)
        /// </summary>
        public GeographCoord End { get; set; }
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
    public class TrassalOutParam
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