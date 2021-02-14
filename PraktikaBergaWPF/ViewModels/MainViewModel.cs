using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using DataCalc;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<GeographCoord> Coords { get; set; }
        public ObservableCollection<Param> Params { get; set; }
        public double Height { get; set; } = 10;
        public double Speed { get; set; } = 200;
        public double Time { get; set; } = 10;
        public double TimeAll { get; set; } = 0;

        public RelayCommand BuildModelCommand { get; }
        public RelayCommand SaveCommand { get; set; }

        public MainViewModel()
        {
            Coords = new ObservableCollection<GeographCoord>(){new GeographCoord(){Fi = 30, Lambda = 40}, new GeographCoord(){Fi = 31, Lambda = 39}};
            Params = new ObservableCollection<Param>();
            BuildModelCommand = new RelayCommand(OnBuildModelCommand);
            SaveCommand = new RelayCommand(OnSaveCommand);
        }

        
        #region Private methods

        private void OnSaveCommand(object Obj)
        {
            var data = String.Join("\n", Params);
            File.WriteAllText("data.txt", data);
        }
        
        private void OnBuildModelCommand(object Obj)
        {
            Task.Run(
                () =>
                {
                    Params = Calc.MakeTrassa(Height * 1000, Speed, Time, Coords.ToList());
                    var sum = 0.0;
                    foreach (var param in Params)
                    {
                        param.Height /= 1000;
                        sum += param.Time;
                    }

                    TimeAll = sum;
                });
        }

        #endregion
        
    }
} 