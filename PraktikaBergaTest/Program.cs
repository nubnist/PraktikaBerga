using System;
using System.Collections.Generic;
using System.IO;
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
                new() {F = 1000, Dt = 1.8, Duration = 100},
                new() {F = 1200, Dt = 1.5, Duration = 100},
                new() {F = 1000, Dt = 1.0, Duration = 100}
            };
            var ran = new List<CharacteristicRAN>()
            {
                new(){Board = CharacteristicRAN.Boards.L, Duration = 3, MinSignal = 500, MaxSignal = 1010, N = 6},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 3, MinSignal = 990, MaxSignal = 2010, N = 6},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 4, MinSignal = 1990, MaxSignal = 4000, N = 6},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 4, MinSignal = 590, MaxSignal = 1010, N = 6},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 3, MinSignal = 990, MaxSignal = 2010, N = 6},
                new(){Board = CharacteristicRAN.Boards.L, Duration = 3, MinSignal = 1990, MaxSignal = 4000, N = 6}
            };
            var res = Calc.MakeStreamTest(new CharacteristicIRI(), stream, new CharacteristicMovingLA(), ran, new Catalog());
            
            File.WriteAllText("potok.txt", res);
        }
    }
}