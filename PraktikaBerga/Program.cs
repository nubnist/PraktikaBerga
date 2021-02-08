using System;
using DataCalc;

namespace PraktikaBerga
{
    class Program
    {
        static void Main(string[] args)
        {
            var coord_beg = new GeographCoord() {Fi = 30, Lambda = 40};
            var coord_end = new GeographCoord() {Fi = 31, Lambda = 39};

            var param = new TrassalInParam()
            {
                h = 10000,
                t = 0,
                V = 200,
                StartGeographCoord = coord_beg,
                EndGeographCoord = coord_end
            };

            var res = Calc.Trassal(param);

            Console.WriteLine($"Широта: {res.CurrentGeographCoord.Fi}, Долгота {res.CurrentGeographCoord.Lambda}"
                              + $", курс {res.Psi}");

        }
    }
}