using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using DataCalc;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<GeographCoord> Coords { get; set; }
        public ObservableCollection<Data> Datas { get; set; }
        public double Height { get; set; }
        public double Speed { get; set; }
        public double Time { get; set; }

        public RelayCommand BuildModelCommand { get; }

        public MainViewModel()
        {
            Coords = new ObservableCollection<GeographCoord>();
            Datas = new ObservableCollection<Data>();
            BuildModelCommand = new RelayCommand(OnBuildModelCommand);
        }

        #region Private methods

        private void OnBuildModelCommand(object Obj)
        {
            MessageBox.Show("hello gay!");
        }

        #endregion
        
    }
} 