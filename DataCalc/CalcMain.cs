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
					var res = cycle_ran // Определение принадлежности к строке параметров системы РЭН
					   .First(i => i.start <= time 
					               && i.end >= time
					               && i.charact.MinSignal <= stream.F
					               && i.charact.MaxSignal >= stream.F);
					if (res.Equals(default(ValueTuple<double, double, CharacteristicRAN>))) break; // Не входит в циклограмму работы РЭН
					
					
				}
			}
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
		public static ObservableCollection<Param> MakeTrassa(
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
			foreach (var p in pl)
			{
				while (true)
				{
					var tst = pl.GetRange(0, pl.IndexOf(p)).Sum(i => i.Time);
					var res = Trassal(new() {h = p.Height, t = tm - tst, Start = p.Start, End = p.End, V = speed});
					(double fi, double lambda) cr = CoordRand(res.CurrentGeographCoord.Fi, res.CurrentGeographCoord.Lambda, location_sigma);
					f.WriteLine(
						new Param()
						{
							Time = Math.Abs((double) RandomNorm(tm, time_sigma)), Fi = cr.fi, Lambda = cr.lambda,
							Height = height, Psi = RandomNorm(res.Psi, psi_sigma), Tangaz = 0, Kren = 0,
						});
					tm += time;
					if (tm <= pl.GetRange(0, pl.IndexOf(p) + 1).Sum(i => i.Time)) continue;
					if (p == pl.Last())
					{
						res = Trassal(new() {h = p.Height, t = p.Time, Start = p.Start, End = p.End, V = speed});
						cr = CoordRand(res.CurrentGeographCoord.Fi, res.CurrentGeographCoord.Lambda, location_sigma);
						var t = Math.Abs((double) RandomNorm(pl.Sum(i => i.Time), time_sigma));
						f.WriteLine(
							new Param()
							{
								Time = t, Fi = cr.fi, Lambda = cr.lambda,
								Height = height, Psi = RandomNorm(res.Psi, psi_sigma), Tangaz = 0, Kren = 0,
							});
						f.WriteLine($"{t:0.000000000}");
					}

					break;
				}
			}

			f.Close();

			return new ObservableCollection<Param>(pl);
		}
	}
}