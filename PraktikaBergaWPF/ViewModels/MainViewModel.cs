using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using DataCalc;
using WpfApp1.Data;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<IRI> Iris { get; set; }
        public IRI SelectedIRI { get; set; }
        public ObservableCollection<GeographCoord> Coords { get; set; }
        public ObservableCollection<Param> Params { get; set; }
        public double Height { get; set; } = 10;
        public double Speed { get; set; } = 200;
        public double Time { get; set; } = 10;
        public double TimeAll { get; set; } = 0;

        public RelayCommand BuildModelCommand { get; }
        public RelayCommand SaveCommand { get; }
        public double TimeSigma { get; set; } = 0;
        public double PsiSigma { get; set; } = 0;
        public double LocationSigma { get; set; } = 0;

        #region EditIRI

        public Visibility EditIriVisibility { get; set; } = Visibility.Collapsed;
        public int EditIriHeight { get; set; } = 0;
        public int EditIriHeightStart { get; set; } = 0;
        public int EditIriHeightEnd { get; set; } = 400;
        public int EditIriHeightAnimationStep { get; set; } = 10;
        private int EditIriHeightAnimationSpeed = 1;
        
        
        public RelayCommand EditIRICommand { get; }
        public RelayCommand SaveIRICommand { get; }
        public RelayCommand AddIRICommand { get; }
        public RelayCommand RemoveIRICommand { get; }

        #endregion

        public MainViewModel()
        {
            Coords = new ObservableCollection<GeographCoord>()
            {
                new GeographCoord(){Fi = 30, Lambda = 40}, 
                new GeographCoord(){Fi = 31, Lambda = 39},
                new GeographCoord(){Fi = 32, Lambda = 36},
                new GeographCoord(){Fi = 33, Lambda = 31},
                new GeographCoord(){Fi = 35, Lambda = 25}
            };
            Params = new ObservableCollection<Param>();
            BuildModelCommand = new RelayCommand(OnBuildModelCommand);
            SaveCommand = new RelayCommand(OnSaveCommand);
            EditIRICommand = new RelayCommand(OnEditIRI, OnSelectedIRICheck);
            
            SaveIRICommand = new RelayCommand(OnSaveIRI);
            RemoveIRICommand = new RelayCommand(OnRemoveIRI, OnSelectedIRICheck);
            AddIRICommand = new RelayCommand(AddIRI);

            Iris = new ObservableCollection<IRI>()
            {
                new IRI()
                {
                    CharacteristicIri = new CharacteristicIRI()
                    {
                        NType = 321
                    }
                },
                new IRI()
                {
                    CharacteristicIri = new CharacteristicIRI()
                    {
                        NType = 321
                    }
                },
                new IRI()
                {
                    CharacteristicIri = new CharacteristicIRI()
                    {
                        NType = 321
                    }
                }
            };
        }

        
        #region Private methods
        
        private void AddIRI(object obj)
        {
            SelectedIRI = new IRI()
            {
                CharacteristicIri = new CharacteristicIRI(),
                IriStream = new CharacteristicStream()
            };
            Iris.Add(SelectedIRI);
            Task.Run(() =>
            {
                EditIriVisibility = Visibility.Visible;
                for (; EditIriHeight <= EditIriHeightEnd; EditIriHeight += EditIriHeightAnimationStep)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(EditIriHeightAnimationSpeed));
                }
                
            });
        }
        
        private void OnRemoveIRI(object obj)
        {
            Iris.Remove(SelectedIRI);
            SelectedIRI = null;

            if (EditIriVisibility == Visibility.Visible)
            {
                Task.Run(() =>
                {
                    for (; EditIriHeight >= EditIriHeightStart; EditIriHeight -= EditIriHeightAnimationStep)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(EditIriHeightAnimationSpeed));
                    }
                    EditIriVisibility = Visibility.Collapsed;
                });
            }
        }

        private bool OnSelectedIRICheck(object obj) => SelectedIRI != null;
        
        private void OnEditIRI(object obj)
        {
            Task.Run(() =>
            {
                EditIriVisibility = Visibility.Visible;
                for (; EditIriHeight <= EditIriHeightEnd; EditIriHeight += EditIriHeightAnimationStep)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(EditIriHeightAnimationSpeed));
                }
                
            });
        }

        private void OnSaveIRI(object obj)
        {
            SelectedIRI = null;
            Task.Run(() =>
            {
                for (; EditIriHeight >= EditIriHeightStart; EditIriHeight -= EditIriHeightAnimationStep)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(EditIriHeightAnimationSpeed));
                }
                EditIriVisibility = Visibility.Collapsed;
            });
        }
        
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
                    Params = new ObservableCollection<Param>(Calc.MakeTrassa(Height, Speed, Time, Coords.ToList(), TimeSigma, PsiSigma));
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