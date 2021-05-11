using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace DataCalc
{
    public static partial class Calc
    {
        private static double RandNormTwoNum(double one, double two)
        {
            var dev = Math.Abs(one - two);
            var randNum = Math.Abs(RandomNorm(0, dev));
            var min = Math.Min(one, two);
            return min + randNum;
        }

        /// <summary>
        /// Процедура формирования пакетов импульсов, излучаемых заданным источником и принимаемых на борту
        /// ЛА, перемещающегося по заданной трассе
        /// </summary>
        /// <param name="charact_iri">Характеристика источника радиоизлучения</param>
        /// <param name="characts_stream">Характеристика излучаемого потока сигналов</param>
        /// <param name="charact_mov_la">Характеристика процесса перемещения летательного аппарата</param>
        /// <param name="characts_ran">Параметры системы радиоэлектронного наблюдения, размещенной на борту ЛА</param>
        /// <param name="catalog">Каталог типов РЭС</param>
        public static (List<Package> all_data, List<List<Package>> packages) MakeStream(
            CharacteristicIRI charact_iri,
            List<CharacteristicStream> characts_stream,
            CharacteristicMovingLA charact_mov_la,
            List<CharacteristicRAN> characts_ran,
            Catalog catalog)
        {
            var results = string.Empty; // Сюда будут сохранятся результаты

			#region Расчет случайных величин при нулевых характеристиках

            foreach (var stream in characts_stream)
            {
                if (stream.F == 0) stream.F = RandNormTwoNum(catalog.FMin, catalog.FMax);
                if (stream.Tau == 0) stream.Tau = RandNormTwoNum(catalog.TauMin, catalog.TauMax);
                if (stream.Dt == 0) stream.Dt = RandNormTwoNum(catalog.DtMin, catalog.DtMax);
            }

			#endregion


            IEnumerable<(double start, double end, CharacteristicRAN charact)> cycle_ran = characts_ran // Определение начал и концов Таблицы 2
               .Select
                (
                    i =>
                    (
                        characts_ran.Take(characts_ran.IndexOf(i)).Sum(j => j.Duration), // Начало подцикла
                        characts_ran.Take(characts_ran.IndexOf(i) + 1).Sum(j => j.Duration), // Конец подцикла
                        i // Элемент подцикла Таблицы 2
                    )
                ).ToList();

            var flight_end = MakeTrassa(
                    charact_mov_la.Height, charact_mov_la.Speed, charact_mov_la.Time, charact_mov_la.Coords).Last()
               .Time * 1000; // Получаю время когда самолет долетит до конечной точки
            var t1 = 0.0;
            var all_data = new List<Package>();
            var packages = new List<List<Package>>();
            while (t1 <= flight_end)
            {
                var t2 = 0.0;
                foreach (var stream in characts_stream)
                {
                    var count = 0;
                    var firstPackageTime = 0.0;
                    CharacteristicRAN previousRan = null;
                    var package = new List<Package>();

                    for (var t3 = 0.0; t3 < stream.Duration; t3 += stream.Dt)
                    {
                        if (t1 + t2 + t3 > flight_end) break;


						#region Подгонка под таблицу 2

                        var t = t1 + t2 + t3; // Время, будет использоваться для синхронизации с таблицей 2
                        var is_ran = cycle_ran // Определение принадлежности к циклограмме работы системы РЭН по времени
                           .FirstOrDefault(
                                i => i.start * 1000 <= t // Умножаю на 1000 чтобы перевести секунды в милесекунды
                                     && i.end * 1000 > t);
                        if (is_ran.Equals(default(ValueTuple<double, double, CharacteristicRAN>)))
                            t = t % (cycle_ran.Last().end * 1000);

						#endregion


						#region Принадлежность к циклограмме работы системы РЭН - Проверка 1

                        is_ran = cycle_ran // Определение принадлежности к циклограмме работы системы РЭН
                           .FirstOrDefault(
                                i => i.start * 1000 <= t // Умножаю на 1000 чтобы перевести секунды в милесекунды
                                     && i.end * 1000 > t
                                     && i.charact.MinSignal <= stream.F
                                     && i.charact.MaxSignal >= stream.F);

                        if (is_ran.Equals(default(ValueTuple<double, double, CharacteristicRAN>)))
                            continue; // Если не подходит по характеристикам переходим на следующую итерацию.

						#endregion


						#region Обращение к процедуре MakeTrassa

                        IEnumerable<Param> trassa;
                        if (t1 + t2 + t3 == 0)
                        {
                            trassa = MakeTrassa(
                                charact_mov_la.Height, charact_mov_la.Speed, stream.Duration / 1000,
                                charact_mov_la.Coords); // Расчет данных о полете ЛА по ломанной с шагом dt
                        }
                        else
                        {
                            try
                            {
                                trassa = MakeTrassa(
                                    charact_mov_la.Height, charact_mov_la.Speed, (t1 + t2 + t3) / 1000,
                                    charact_mov_la.Coords); // Расчет данных о полете ЛА по ломанной с шагом dt
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }

                        }
                        var trassa_point =
                            trassa.FirstOrDefault(i => ($"{(i.Time * 1000):0.000000000}" == $"{(t1 + t2 + t3):0.000000000}")) // Определение данных ЛА в текущий момент
                            ?? throw new Exception("Такой точки нету");

						#endregion


						#region Общие расчеты - геоцентрические координаты ИРИ и ЛА, координаты вектора от ЛА к ИРИ, расстояние между ЛА и ИРИ

                        var p_iri = new GeocentrCoord() // Геоцентрические координаты ИРИ
                        {
                            X = R * Cos(charact_iri.Coord.Fi) * Cos(charact_iri.Coord.Lambda),
                            Y = R * Cos(charact_iri.Coord.Fi) * Sin(charact_iri.Coord.Lambda),
                            Z = R * Sin(charact_iri.Coord.Fi)
                        };
                        var p_la = trassa_point.p;
                        var p_la_iri = p_iri - p_la; // Геоцентрические координаты вектора, направленного от ЛА к ИРИ
                        var r_la_iri = GeocentrCoord.Abs(p_la_iri); // Расстояние между ЛА и ИРИ

						#endregion


						#region Проверка на нахождение в пределах радиовидимости - Проверка 2

                        var D = 1.15 * Math.Sqrt(Math.Pow(R + trassa_point.Height, 2) - R * R); // Дальность радиогаризонта
                        if (r_la_iri > D) continue; // ИРИ НЕ находится в пределах радиовидимости ЛА - на следующую итерацию

						#endregion


						#region Проверка борта приема - Проверка 3

                        double[,] matrix =
                        {
                            {
                                p_la_iri.X, p_la_iri.Y, p_la_iri.Z
                            },
                            {
                                trassa_point.v.X, trassa_point.v.Y, trassa_point.v.Z
                            },
                            {
                                0, 1, 0
                            }
                        };
                        var delta_la_iri = Determ(matrix);
                        var board = delta_la_iri < 0 ? CharacteristicRAN.Boards.L : CharacteristicRAN.Boards.R; // Определение борта
                        if (is_ran.charact.Board != board) // Если знак не соответствует бортам - следующая итерация
                            continue;

						#endregion


						#region Проверка угла пеленга - Проверка 4

                        var c = (p_la_iri * trassa_point.v) / r_la_iri; // Косинус угла пеленга
                        //if (is_ran.charact.BMin > c || is_ran.charact.BMax < c) continue; // Если угол пеленга не попадает в диапазон - выход из цикла

						#endregion

                        // Заполнение массива со всеми излученными сигналами
                        all_data.Add(new Package()
                        {
                            Time = (t1 + t2 + t3) / 1000.0,
                            Fi = trassa_point.Fi,
                            Lambda = trassa_point.Lambda,
                            Height = trassa_point.Height,
                            Psi = trassa_point.Psi < 0 ? 360 + trassa_point.Psi : trassa_point.Psi,
                            Tangaz = trassa_point.Tangaz,
                            Kren = trassa_point.Kren,
                            Board = board,
                            F = stream.F,
                            Tau = stream.Tau
                        });
                        

						#region Проверка на уже оформленные пакеты - Проверка 5

                        if (is_ran.charact == previousRan)
                        {
                            if (count <= is_ran.charact.N)
                            {
                                count++;
                                var pack = new Package()
                                {
                                    Time = (t1 + t2 + t3) / 1000.0,
                                    Fi = trassa_point.Fi,
                                    Lambda = trassa_point.Lambda,
                                    Height = trassa_point.Height,
                                    Psi = trassa_point.Psi < 0 ? 360 + trassa_point.Psi : trassa_point.Psi,
                                    Tangaz = trassa_point.Tangaz,
                                    Kren = trassa_point.Kren,
                                    Board = board,
                                    F = stream.F,
                                    Tau =  stream.Tau
                                };
                                package.Add(pack);

                                var current_data = $"{(t1 + t2 + t3) / 1000.0:0.000000000}\t{trassa_point.Fi:0.00000}\t{trassa_point.Lambda:0.00000}"
                                                   + $"\t{trassa_point.Height / 1000:0.000}\t{(trassa_point.Psi < 0 ? 360 + trassa_point.Psi : trassa_point.Psi):0.00000}\t{trassa_point.Tangaz:0.00000}\t{trassa_point.Kren:0.00000}\t"
                                                   + $"{board}\t{c:0.00000}\t{stream.F:0.0}\t{stream.Tau:0.000}\tABC\n";
                                results += current_data;
                                Console.Write(current_data);
                            }
                            else
                            {
                                previousRan = is_ran.charact;
                                packages.Add(package);
                                break;
                            }
                        }
                        else
                        {
                            count = 1;
                            if (package.Any())
                            {
                                packages.Add(package);
                                package.Clear();
                            }
                            var current_data = $"{(t1 + t2 + t3) / 1000.0:0.000000000}\t{trassa_point.Fi:0.00000}\t{trassa_point.Lambda:0.00000}"
                                               + $"\t{trassa_point.Height / 1000:0.000}\t{(trassa_point.Psi < 0 ? 360 + trassa_point.Psi : trassa_point.Psi):0.00000}\t{trassa_point.Tangaz:0.00000}\t{trassa_point.Kren:0.00000}\t"
                                               + $"{board}\t{c:0.00000}\t{stream.F:0.0}\t{stream.Tau:0.000}\tABC\n";
                            results += current_data;
                            Console.Write(current_data);
                        }

						#endregion


                        /*
                        #region Формирование вывода в файл

                        var current_data = $"{(t1 + t2 + t3) / 1000.0:0.000000000}\t{trassa_point.Fi:0.00000}\t{trassa_point.Lambda:0.00000}"
                                           + $"\t{trassa_point.Height / 1000:0.000}\t{(trassa_point.Psi < 0 ? 360 + trassa_point.Psi : trassa_point.Psi):0.00000}\t{trassa_point.Tangaz:0.00000}\t{trassa_point.Kren:0.00000}\t"
                                           + $"{board}\t{c:0.00000}\t{stream.F:0.0}\t{stream.Tau:0.000}\tABC\n";
                        results += current_data;
                        Console.Write(current_data);

                        #endregion
                        */

                        previousRan = is_ran.charact;
                    }
                    Console.WriteLine("---------------------------");
                    t2 += stream.Duration;
                }

                t1 += t2;
            }

            return (all_data, packages);
        }

        /// <summary>
        /// Определитель матрицы NxN
        /// </summary>
        /// <param name="matrix">Матрицы NxN</param>
        /// <returns>Определитель матрицы</returns>
        /// <exception cref="Exception">Проверка на матрицу NxN</exception>
        public static double Determ(double[,] matrix)
        {
            if (matrix.GetLength(0) != matrix.GetLength(1)) throw new Exception(" Число строк в матрице не совпадает с числом столбцов");
            double det = 0;
            int Rank = matrix.GetLength(0);
            if (Rank == 1) det = matrix[0, 0];
            if (Rank == 2) det = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
            if (Rank > 2)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    det += Math.Pow(-1, 0 + j) * matrix[0, j] * Determ(GetMinor(matrix, 0, j));
                }
            }

            return det;
        }
        public static double[,] GetMinor(double[,] matrix, int row, int column)
        {
            if (matrix.GetLength(0) != matrix.GetLength(1)) throw new Exception(" Число строк в матрице не совпадает с числом столбцов");
            double[,] buf = new double[matrix.GetLength(0) - 1, matrix.GetLength(0) - 1];
            for (int i = 0; i < matrix.GetLength(0); i++)
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if ((i != row) || (j != column))
                    {
                        if (i > row && j < column) buf[i - 1, j] = matrix[i, j];
                        if (i < row && j > column) buf[i, j - 1] = matrix[i, j];
                        if (i > row && j > column) buf[i - 1, j - 1] = matrix[i, j];
                        if (i < row && j < column) buf[i, j] = matrix[i, j];
                    }
                }
            return buf;
        }

        
        /// <summary>
        /// Разчет массивов данных разных ИРИ с заданными отклонениями
        /// </summary>
        /// <param name="ran_data">Трассы летательного аппаратат для различных ИРИ</param>
        /// <param name="time_sigma">Погрешность времени</param>
        /// <param name="coord_sigma">Погрешность координат</param>
        /// <param name="height_sigma">Погрешность высоты</param>
        /// <param name="psi_sigma">Погрешность курсового угла</param>
        /// <param name="c_sigma">Погрешность косинуса угла пеленга</param>
        /// <param name="f_sigma">Погрешность несущей частоты</param>
        /// <param name="tau_sigma">Погрешность длительности сигнала</param>
        /// <returns>
        /// Массив1 - все импульсы пакетов без погрешностей,
        /// Массив2 - массив усредненых пакетов без погрешности,
        /// Массив3 - все импульсы пакетов с погрешностью,
        /// Массив4 - массив усредненых пакетов с погрешностью
        /// </returns>
        public static (List<Package> arr1, 
            List<Package> arr2, 
            List<Package> arr3, 
            List<Package> arr4) RanUnion(List<(List<Package> all_data, List<List<Package>> packages)> ran_data, 
            double time_sigma, double fi_sigma, double coord_sigma, double height_sigma, double psi_sigma,
            double c_sigma, double f_sigma, double tau_sigma)
        {
            var arr1 = new List<Package>(); // Массив всех импульсов пакетов без погрешности
            var arr2 = new List<Package>(); // Массив всех усредненных пакетов без погрешности
            foreach (var data in ran_data) // Заполнение массива 1 импульсами различных ИРИ
            {
                arr1.AddRange(data.packages.SelectMany(i => i));
                arr2.AddRange(data.packages.Select(i => new Package()
                {
                    Time = i.First().Time, Fi = i.Average(j => j.Fi),
                    Lambda = i.Average(j => j.Lambda), Height = i.First().Height,
                    Psi = i.Average(j => j.Psi), 
                    Tangaz = i.Average(j => j.Tangaz),
                    Kren = i.Average(j => j.Kren),
                    Board = i.First().Board, С = i.Average(j => j.С),
                    F = i.Average(j => j.F),
                    Tau = i.Average(j => j.Tau),
                    Type = i.First().Type,
                    Number = i.First().Number
                }));
            }
            var arr3 = // Массив всех усредненных импульсов пакетов c погрешностью
                IriDataSigma(arr1, time_sigma, coord_sigma, 
                height_sigma, psi_sigma, c_sigma, 
                f_sigma, tau_sigma); 
            var arr4 = // Массив всех усредненных пакетов c погрешностью
                IriDataSigma(arr2, time_sigma, coord_sigma, 
                height_sigma, psi_sigma, c_sigma, 
                f_sigma, tau_sigma);
            
            // Сортировка массивов по времени
            arr1 = arr1.OrderBy(i => i.Time).ToList(); // Сортировка по времени
            arr2 = arr2.OrderBy(i => i.Time).ToList(); // Сортировка по времени
            arr3 = arr3.OrderBy(i => i.Time).ToList(); // Сортировка по времени
            arr4 = arr4.OrderBy(i => i.Time).ToList(); // Сортировка по времени
            

            return default;
        }
        private static IEnumerable<Package> IriDataSigma(List<Package> packages, double time_sigma, double coord_sigma, double height_sigma, double psi_sigma, double c_sigma, double f_sigma, double tau_sigma)
        {

            return packages                 // Массив всех импульсов пакетов c погрешностью
               .Select(i => new Package()
                {
                    Time = Math.Abs((double)RandomNorm(i.Time, time_sigma)), 
                    Fi = CoordRand(i.Fi, i.Lambda, coord_sigma).Fi,
                    Lambda = CoordRand(i.Fi, i.Lambda, coord_sigma).Lambda, 
                    Height = (double)RandomNorm(i.Height, height_sigma),
                    Psi = (double)RandomNorm(i.Psi, psi_sigma), 
                    Tangaz = i.Tangaz,
                    Kren = i.Kren,
                    Board = i.Board, 
                    С = (double)RandomNorm(i.С, c_sigma),
                    F = (double)RandomNorm(i.F, f_sigma),
                    Tau = (double)RandomNorm(i.Tau, tau_sigma),
                    Type = i.Type,
                    Number = i.Number
                });
        }

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
                (Math.Min(param.Start.Fi, param.End.Fi) > Math.Round(f, 6)
                 || Math.Round(f, 6) > Math.Max(param.Start.Fi, param.End.Fi)
                 || Math.Min(param.Start.Lambda, param.End.Lambda) > Math.Round(lambda, 6)
                 || Math.Round(lambda, 6) > Math.Max(param.Start.Lambda, param.End.Lambda))
                && Math.Abs(param.Start.Fi - param.End.Fi) >= 0.000000001 && Math.Abs(param.Start.Lambda - param.End.Lambda) >= 0.000000001
            )
            {
                return null;
            }
            if
            (
                (Math.Min(param.Start.Lambda, param.End.Lambda) > Math.Round(lambda, 6)
                 || Math.Round(lambda, 6) > Math.Max(param.Start.Lambda, param.End.Lambda))
                && Math.Abs(param.Start.Fi - param.End.Fi) < 0.000000001
            )
            {
                return null;
            }
            if
            (
                (Math.Min(param.Start.Fi, param.End.Fi) > Math.Round(f, 6)
                 || Math.Round(f, 6) > Math.Max(param.Start.Fi, param.End.Fi)
                 || Math.Min(param.Start.Lambda, param.End.Lambda) > Math.Round(lambda, 6))
                && Math.Abs(param.Start.Lambda - param.End.Lambda) < 0.000000001
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
                X = -Sin(f) * Cos(lambda), Y = -Sin(f) * Sin(lambda), Z = Cos(f)
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
                CurrentGeographCoord = new GeographCoord()
                {
                    Fi = f, Lambda = lambda
                },
                Psi = psi,
                p = new GeocentrCoord()
                {
                    X = p.X, Y = p.Y, Z = p.Z
                },
                v = v
            };
        }


        /// <summary>
        /// Формирование данных о пролете ЛА по ломанной
        /// </summary>
        /// <param name="height">Высота полета (км)</param>
        /// <param name="speed">Скорость полета (м/сек)</param>
        /// <param name="time">Шаг времени (сек)</param>
        /// <param name="coords">Координаты ломанной</param>
        /// <param name="time_accuracy">Точность по времени</param>
        /// <returns>Данные о пролете по ломанной</returns>
        public static IEnumerable<Param> MakeTrassa(
            double height, double speed, double time, List<GeographCoord> coords,
            double time_sigma = 0, double psi_sigma = 0, double location_sigma = 0, double time_accuracy = 0.000001)
        {
            height = height * 1000;
            var in_param = new TrassalInParam()
            {
                h = height, V = speed
            };
            var pl = new List<Param>();
            double tm;
            for (var i = 1; i < coords.Count; i++)
            {
                TrassalOutParam res;
                tm = time;

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

                pl.Add(
                    new Param()
                    {
                        Fi = res.CurrentGeographCoord.Fi,
                        Lambda = res.CurrentGeographCoord.Lambda,
                        Height = height,
                        Psi = res.Psi,
                        Time = in_param.t,
                        Kren = 0,
                        Tangaz = 0,
                        Start = new GeographCoord()
                        {
                            Fi = coords[i - 1].Fi, Lambda = coords[i - 1].Lambda
                        },
                        End = new GeographCoord()
                        {
                            Fi = coords[i].Fi, Lambda = coords[i].Lambda
                        }
                    });
            }

            //StreamWriter f = new StreamWriter("data.txt");
            tm = 0;
            IList<Param> result = new List<Param>();
            foreach (var p in pl)
            {
                while (true)
                {
                    Param r;
                    var tst = pl.GetRange(0, pl.IndexOf(p)).Sum(i => i.Time);
                    var res = Trassal(new()
                    {
                        h = p.Height,
                        t = tm - tst,
                        Start = p.Start,
                        End = p.End,
                        V = speed
                    });
                    if (res is null)
                    {
                        break;
                    }
                    (double fi, double lambda) cr = CoordRand(res.CurrentGeographCoord.Fi, res.CurrentGeographCoord.Lambda, location_sigma);
                    r = new Param()
                    {
                        Time = Math.Abs((double) RandomNorm(tm, time_sigma)),
                        Fi = cr.fi,
                        Lambda = cr.lambda,
                        Height = height,
                        Psi = RandomNorm(res.Psi, psi_sigma),
                        Tangaz = 0,
                        Kren = 0,
                        v = res.v,
                        p = res.p
                    };
                    result.Add(r);
                    //f.WriteLine(r);
                    tm += time;
                    if (tm <= pl.GetRange(0, pl.IndexOf(p) + 1).Sum(i => i.Time)) continue;
                    if (p == pl.Last())
                    {
                        res = Trassal(new()
                        {
                            h = p.Height,
                            t = p.Time,
                            Start = p.Start,
                            End = p.End,
                            V = speed
                        });
                        cr = CoordRand(res.CurrentGeographCoord.Fi, res.CurrentGeographCoord.Lambda, location_sigma);
                        var t = Math.Abs((double) RandomNorm(pl.Sum(i => i.Time), time_sigma));
                        r = new Param()
                        {
                            Time = t,
                            Fi = cr.fi,
                            Lambda = cr.lambda,
                            Height = height,
                            Psi = RandomNorm(res.Psi, psi_sigma),
                            Tangaz = 0,
                            Kren = 0,
                            v = res.v,
                            p = res.p
                        };
                        result.Add(r);
                        //f.WriteLine(r);
                        //f.WriteLine($"{t:0.000000000}");
                    }

                    break;
                }
            }

            //f.Close();

            return result;
        }
    }
}