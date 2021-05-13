using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using DataCalc;

namespace PraktikaBerga
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var stream = new List<CharacteristicStream>()
            {
                new() {F = 1000, Tau = 1.2, Dt = 1.8, Duration = 100},
                new() {F = 1200, Tau = 1.2, Dt = 1.5, Duration = 100},
                new() {F = 1000, Tau = 1.2, Dt = 1.0, Duration = 100}
            };
            var ran = new List<CharacteristicRAN>()
            {
                new(){Board = CharacteristicRAN.Boards.L, Duration = 3, MinSignal = 500, MaxSignal = 1010, N = 5},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 3, MinSignal = 990, MaxSignal = 2010, N = 5},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 4, MinSignal = 1990, MaxSignal = 4000, N = 5},
                new(){Board = CharacteristicRAN.Boards.R, Duration = 4, MinSignal = 590, MaxSignal = 1010, N = 5},
                new(){Board = CharacteristicRAN.Boards.R, Duration = 3, MinSignal = 990, MaxSignal = 2010, N = 5},
                new(){Board = CharacteristicRAN.Boards.R, Duration = 3, MinSignal = 1990, MaxSignal = 4000, N = 5}
            };
            var iri = new CharacteristicIRI()
            {
              Coord  = new GeographCoord(){Fi = 59.3, Lambda = 40.3}
            };
            var la = new CharacteristicMovingLA()
            {
                Coords = new List<GeographCoord>()
                {
                    new GeographCoord(){Fi = 59, Lambda = 39}, 
                    new GeographCoord(){Fi = 59, Lambda = 39.5},
                },
                Height = 10,
                Speed = 200,
                Time = 1
            };
            var res = Calc.MakeStream(iri, stream, la, ran, new Catalog());

            int b = 5;
            //File.WriteAllText("potok.txt", res);
        }
    }
}