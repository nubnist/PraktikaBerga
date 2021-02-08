using System;
using System.Threading.Channels;
using DataCalc;

namespace PraktikaBerga
{
    class Program
    {
        static void Main(string[] args)
        {
            var coord_beg = new GeographCoord() {Fi = 30, Lambda = 40};
            var coord_end = new GeographCoord() {Fi = 31, Lambda = 39};
            var h = 10000;
            var V = 200;

            Console.WriteLine("t\tШирота\t Долгота\t\tКурс\t\t\tВектор Po\t\t\tВектор v");
            for (var i = 0; i <= 60; i+=10)
            {
                var param = new TrassalInParam()
                {
                    h = h,
                    t = i,
                    V = V,
                    StartGeographCoord = coord_beg,
                    EndGeographCoord = coord_end
                };
                var res = Calc.Trassal(param);
                Console.WriteLine($"{i}\t"
                                  + $"{res.CurrentGeographCoord.Fi:0.00000} {res.CurrentGeographCoord.Lambda:0.00000}\t\t"
                                  + $"{res.Psi:0.00000}\t\t"
                                  + $"{res.p.X:0.00} {res.p.Y:0.00} {res.p.Z:0.00}\t\t"
                                  + $"{res.v.X:0.00000} {res.v.Y:0.00000} {res.v.Z:0.00000}");
            }
        }
    }
}