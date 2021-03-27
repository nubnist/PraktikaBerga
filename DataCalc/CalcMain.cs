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
		/// <summary>
		/// Процедура формирования пакетов импульсов, излучаемых заданным источником и принимаемых на борту
		/// ЛА, перемещающегося по заданной трассе
		/// </summary>
		/// <param name="charact_iri">Характеристика источника радиоизлучения</param>
		/// <param name="characts_stream">Характеристика излучаемого потока сигналов</param>
		/// <param name="charact_mov_la">Характеристика процесса перемещения летательного аппарата</param>
		/// <param name="characts_ran">Параметры системы радиоэлектронного наблюдения, размещенной на борту ЛА</param>
		/// <param name="catalog">Каталог типов РЭС</param>
		public static void MakeStream(
			CharacteristicIRI charact_iri,
			List<CharacteristicStream> characts_stream,
			CharacteristicMovingLA charact_mov_la,
			List<CharacteristicRAN> characts_ran, Catalog catalog)
		{
			//TODO: Для характеристик излучаемого потока сигналов сделать случаное значение для нулевых элементов
			IEnumerable<(double start, double end, CharacteristicRAN charact)> cycle_ran = characts_ran // Определение начал и концов подциклов
			   .Select
				(
					i =>
					(
						characts_ran.Take(characts_ran.IndexOf(i)).Sum(j => j.Duration), // Начало подцикла
						characts_ran.Take(characts_ran.IndexOf(i) + 1).Sum(j => j.Duration), // Конец подцикла
						i // Элемент подцикла
					)
				);

			double time = 0; // Сквозное время
			double time_cyclogram = 0; // Общее время продолжительности подциклов
			foreach (var stream in characts_stream) // Перебор циклограммы процесса излучения ИРИ
			{
				time_cyclogram += stream.Duration; // Увеличение общей продолжительности подциклов на продолжительность текущего подцикла
				var dur = stream.Tau + stream.Dt; // Шаг излучения (длительность импульса + межимпульсный интервал)
				for (; time <= time_cyclogram; time += dur) // Перебор импульсов в подцикле
				{
					var is_ran = cycle_ran // Определение принадлежности к циклограмме работы системы РЭН
					   .FirstOrDefault(
							i => i.start * 1000 <= time // Умножаю на 1000 чтобы перевести секунды в милесекунды
							     && i.end * 1000 >= time
							     && i.charact.MinSignal <= stream.F
							     && i.charact.MaxSignal >= stream.F);
					if (is_ran.Equals(default(ValueTuple<double, double, CharacteristicRAN>))) break; // Не входит в циклограмму работы РЭН - выходим из цикла
					var trassa =
						MakeTrassa(charact_mov_la.Height, charact_mov_la.Speed, dur, charact_mov_la.Coords); // Расчет данных о полете ЛА по ломанной
					var current_data_la = trassa.FirstOrDefault(i => i.Time.Equals(time)); // Поиск данных для текущего времени
					if (current_data_la is null) break; // Если такого времени в расчетах нет, то выходим из цикла
					var p_iri = new GeocentrCoord() // Геоцентрические координаты ИРИ
					{
						X = R * Cos(charact_iri.Coord.Fi) * Cos(charact_iri.Coord.Lambda),
						Y = R * Cos(charact_iri.Coord.Fi) * Sin(charact_iri.Coord.Lambda),
						Z = R * Sin(charact_iri.Coord.Fi)
					};
					var p_la = new GeocentrCoord() // Геоцентрические координаты ЛА
					{
						X = R * Cos(current_data_la.Fi) * Cos(current_data_la.Lambda),
						Y = R * Cos(current_data_la.Fi) * Sin(current_data_la.Lambda),
						Z = R * Sin(current_data_la.Fi)
					};
					var D = 1.15 * Math.Sqrt(Math.Pow(R + current_data_la.Height, 2) - R * R); // Дальность радиогаризонта
					//TODO: Добавить проверку на нахождение в пределах радиовидимости ЛА
					var p_la_iri = p_iri - p_la; // Геоцентрические координаты вектора, направленного от ЛА к ИРИ
					var r_la_iri = GeocentrCoord.Abs(p_la_iri); // Расстояние между ЛА и ИРИ
					var c = (p_la_iri * current_data_la.v) / r_la_iri; // Косинус угла пеленга
					if (is_ran.charact.BMin > c || is_ran.charact.BMax < c) break; // Если угол пеленга не попадает в диапазон - выход из цикла
					var delta_la_iri = // Борт приема сигнала TODO: убрать нули - уберу после отладки
						p_la_iri.X * current_data_la.v.Y * 0 +
						p_la_iri.Y * current_data_la.v.Z * 0 +
						p_la_iri.Z * current_data_la.v.X * 1 -
						p_la_iri.Z * current_data_la.v.Z * 0 -
						p_la_iri.Y * current_data_la.v.X * 0 -
						p_la_iri.X * current_data_la.v.Z * 1;
					if (delta_la_iri < 0 && is_ran.charact.Board == CharacteristicRAN.Boards.R || // Если знае не соответствует бортам - выход из цикла
					    delta_la_iri > 0 && is_ran.charact.Board == CharacteristicRAN.Boards.L)
						break;
					//TODO: Необходимо реализовать пятую проверку - сравнение с аналогичной парой, соответствующей последнему из "оформленных пакетов"
				}
			}
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
		public static string MakeStreamTest(
			CharacteristicIRI charact_iri,
			List<CharacteristicStream> characts_stream,
			CharacteristicMovingLA charact_mov_la,
			List<CharacteristicRAN> characts_ran, Catalog catalog)
		{
			var results = string.Empty; // Сюда будут сохранятся результаты

			//TODO: Для характеристик излучаемого потока сигналов сделать случаное значение для нулевых элементов
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
			   .Time; // Получаю время когда самолет долетит до конечной точки
			var t1 = 0.0;
			while (t1 < flight_end)
			{
				var t2 = 0.0;
				foreach (var stream in characts_stream)
				{
					var packages_counter = 0;
					for (var t3 = 0.0; t3 < stream.Duration; t3 += stream.Dt)
					{
						#region Подгонка под таблицу 2

						var t = t1 + t2 + t3; // Время, кудет использоваться для синхронизации с таблицей 2
						var is_ran = cycle_ran // Определение принадлежности к циклограмме работы системы РЭН по времени
						   .FirstOrDefault(
								i => i.start * 1000 <= t // Умножаю на 1000 чтобы перевести секунды в милесекунды
								     && i.end * 1000 > t);
						if (is_ran.Equals(default(ValueTuple<double, double, CharacteristicRAN>)))
							t = t % (cycle_ran.Last().end * 1000);

						#endregion

						is_ran = cycle_ran // Определение принадлежности к циклограмме работы системы РЭН
						   .FirstOrDefault(
								i => i.start * 1000 <= t // Умножаю на 1000 чтобы перевести секунды в милесекунды
								     && i.end * 1000 > t
								     && i.charact.MinSignal <= stream.F
								     && i.charact.MaxSignal >= stream.F);
						if (is_ran.Equals(default(ValueTuple<double, double, CharacteristicRAN>)))
							continue; // Если не подходит по характеристикам, прерываем цикл.

						var trassa =
							MakeTrassa(
								charact_mov_la.Height, charact_mov_la.Speed, stream.Dt,
								charact_mov_la.Coords); // Расчет данных о полете ЛА по ломанной с шагом dt
						var trassa_point = trassa.FirstOrDefault(i => i.Time.Equals(t1 + t2 + t3)); // Определение данных ЛА в текущий момент
						//if (trassa_point is null) continue; // Если такого времени в расчетах нет, то переходим на следующую итерацию
						var p_iri = new GeocentrCoord() // Геоцентрические координаты ИРИ
						{
							X = R * Cos(charact_iri.Coord.Fi) * Cos(charact_iri.Coord.Lambda),
							Y = R * Cos(charact_iri.Coord.Fi) * Sin(charact_iri.Coord.Lambda),
							Z = R * Sin(charact_iri.Coord.Fi)
						};
						var p_la = new GeocentrCoord() // Геоцентрические координаты ЛА
						{
							X = R * Cos(trassa_point.Fi) * Cos(trassa_point.Lambda),
							Y = R * Cos(trassa_point.Fi) * Sin(trassa_point.Lambda),
							Z = R * Sin(trassa_point.Fi)
						};
						var D = 1.15 * Math.Sqrt(Math.Pow(R + trassa_point.Height, 2) - R * R); // Дальность радиогаризонта
						var p_la_iri = p_iri - p_la; // Геоцентрические координаты вектора, направленного от ЛА к ИРИ
						var r_la_iri = GeocentrCoord.Abs(p_la_iri); // Расстояние между ЛА и ИРИ
						if (r_la_iri > D) continue; // ИРИ НЕ находится в пределах радиовидимости ЛА
						
						var c = (p_la_iri * trassa_point.v) / r_la_iri; // Косинус угла пеленга
						if (is_ran.charact.BMin > c || is_ran.charact.BMax < c) continue; // Если угол пеленга не попадает в диапазон - выход из цикла
						var delta_la_iri = // Борт приема сигнала TODO: убрать нули - уберу после отладки
							p_la_iri.X * trassa_point.v.Y * 0 +
							p_la_iri.Y * trassa_point.v.Z * 0 +
							p_la_iri.Z * trassa_point.v.X * 1 -
							p_la_iri.Z * trassa_point.v.Z * 0 -
							p_la_iri.Y * trassa_point.v.X * 0 -
							p_la_iri.X * trassa_point.v.Z * 1;
						var board = delta_la_iri < 0 ? CharacteristicRAN.Boards.L : CharacteristicRAN.Boards.R; // Определение борта
						if (is_ran.charact.Board != board) // Если знак не соответствует бортам - выход из цикла
							continue;
						
						//TODO: Необходимо реализовать пятую проверку - сравнение с аналогичной парой, соответствующей последнему из "оформленных пакетов"
						results += $"{(t1 + t2 + t3) / 1000.0:0.000000000}\t{trassa_point.Fi}\t{trassa_point.Lambda}"
						           + $"\t{trassa_point.Height}\t{trassa_point.Psi}\t{trassa_point.Tangaz}\t{trassa_point.Kren}\t"
						           + $"{board}\t{c}\t{stream.F}\t{stream.Tau}\tБез каталога";
						if (++packages_counter == is_ran.charact.N) break; // Если достигнуто заданное число пакетов то выходим из перебора импульсов
					}

					t2 += stream.Duration;
				}

				t1 += t2;
			}

			return results;
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
				X = -Sin(f) * Cos(lambda),
				Y = -Sin(f) * Sin(lambda),
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
				CurrentGeographCoord = new GeographCoord() {Fi = f, Lambda = lambda},
				Psi = psi,
				p = new GeocentrCoord() {X = p.X / 1000, Y = p.Y / 1000, Z = p.Z / 1000},
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
		public static IEnumerable<Param> MakeTrassa(
			double height, double speed, double time, List<GeographCoord> coords,
			double time_sigma = 0, double psi_sigma = 0, double location_sigma = 0, double time_accuracy = 0.001)
		{
			var in_param = new TrassalInParam() {h = height, V = speed};
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
						Start = new GeographCoord() {Fi = coords[i - 1].Fi, Lambda = coords[i - 1].Lambda},
						End = new GeographCoord() {Fi = coords[i].Fi, Lambda = coords[i].Lambda}
					});
			}

			StreamWriter f = new StreamWriter("data.txt");
			tm = 0;
			IList<Param> result = new List<Param>();
			foreach (var p in pl)
			{
				while (true)
				{
					Param r;
					var tst = pl.GetRange(0, pl.IndexOf(p)).Sum(i => i.Time);
					var res = Trassal(new() {h = p.Height, t = tm - tst, Start = p.Start, End = p.End, V = speed});
					(double fi, double lambda) cr = CoordRand(res.CurrentGeographCoord.Fi, res.CurrentGeographCoord.Lambda, location_sigma);
					r = new Param()
					{
						Time = Math.Abs((double) RandomNorm(tm, time_sigma)), Fi = cr.fi, Lambda = cr.lambda,
						Height = height, Psi = RandomNorm(res.Psi, psi_sigma), Tangaz = 0, Kren = 0, v = res.v
					};
					result.Add(r);
					f.WriteLine(r);
					tm += time;
					if (tm <= pl.GetRange(0, pl.IndexOf(p) + 1).Sum(i => i.Time)) continue;
					if (p == pl.Last())
					{
						res = Trassal(new() {h = p.Height, t = p.Time, Start = p.Start, End = p.End, V = speed});
						cr = CoordRand(res.CurrentGeographCoord.Fi, res.CurrentGeographCoord.Lambda, location_sigma);
						var t = Math.Abs((double) RandomNorm(pl.Sum(i => i.Time), time_sigma));
						r = new Param()
						{
							Time = t, Fi = cr.fi, Lambda = cr.lambda,
							Height = height, Psi = RandomNorm(res.Psi, psi_sigma), Tangaz = 0, Kren = 0, v = res.v
						};
						result.Add(r);
						f.WriteLine(r);
						f.WriteLine($"{t:0.000000000}");
					}

					break;
				}
			}

			f.Close();

			return result;
		}
	}
}