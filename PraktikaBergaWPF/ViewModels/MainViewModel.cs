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
using Microsoft.Scripting.Utils;
using WpfApp1.Data;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<IRI> Iris { get; set; }
        public IRI SelectedIRI { get; set; }
        public ObservableCollection<GeographCoord> Coords { get; set; }
        public ObservableCollection<Package> Packages { get; set; } = new ObservableCollection<Package>();
        public double Height { get; set; } = 10;
        public double Speed { get; set; } = 200;
        public double Time { get; set; } = 10;

        public RelayCommand BuildModelCommand { get; }
        public RelayCommand Modeling { get; }
        public double TimeSigma { get; set; } = 0;
        public double Bmax { get; set; } = 1;
        public double Bmin { get; set; } = -1;
        public int Npak { get; set; } = 5;
        public double PsiSigma { get; set; } = 0;
        public double CoordSigma { get; set; } = 0;
        public double HeightSigma { get; set; } = 0;
        public double CSigma { get; set; } = 0;
        public double TauSigma { get; set; } = 0;
        public double FSigma { get; set; } = 0;

        public ObservableCollection<CharacteristicRAN> Rans { get; set; } = new ObservableCollection<CharacteristicRAN>();

        #region EditIRI

        public Visibility EditIriVisibility { get; set; } = Visibility.Collapsed;
        public int EditIriHeight { get; set; } = 0;
        public int EditIriHeightStart { get; set; } = 0;
        public int EditIriHeightEnd { get; set; } = 900;
        public int EditIriHeightAnimationStep { get; set; } = 10;
        private int EditIriHeightAnimationSpeed = 1;


        public RelayCommand EditIRICommand { get; }
        public RelayCommand SaveIRICommand { get; }
        public RelayCommand AddIRICommand { get; }
        public RelayCommand RemoveIRICommand { get; }

        #endregion

        public MainViewModel()
        {
            Height = 10;
            Speed = 200;
            Time = 1;
            Coords = new ObservableCollection<GeographCoord>()
            {
                new GeographCoord()
                {
                    Fi = 59, Lambda = 39
                },
                new GeographCoord()
                {
                    Fi = 59, Lambda = 39.5
                },
            };
            Iris = new ObservableCollection<IRI>()
            {
                new IRI()
                {
                    CharacteristicIri = new CharacteristicIRI()
                    {
                        Type = "1 - ABC37", 
                        NType = 1,
                        Coord = new GeographCoord()
                        {
                            Fi = 59.3, Lambda = 40.3
                        }
                    },
                    IriStream = new List<CharacteristicStream>()
                    {
                        new()
                        {
                            F = 1000, Tau = 1.2, Dt = 1.8, Duration = 100
                        },
                        new()
                        {
                            F = 1200, Tau = 1.2, Dt = 1.5, Duration = 100
                        },
                        new()
                        {
                            F = 1000, Tau = 1.2, Dt = 1.0, Duration = 100
                        }
                    }
                },
                
                new IRI()
                {
                    CharacteristicIri = new CharacteristicIRI()
                    {
                        Type = "2 - Q58", 
                        NType = 2,
                        Coord = new GeographCoord()
                        {
                            Fi = 56.3, Lambda = 40.7
                        }
                    },
                    IriStream = new List<CharacteristicStream>()
                    {
                        new()
                        {
                            F = 1000, Tau = 1.2, Dt = 1.8, Duration = 100
                        },
                        new()
                        {
                            F = 1200, Tau = 1.2, Dt = 1.5, Duration = 100
                        },
                        new()
                        {
                            F = 1000, Tau = 1.2, Dt = 1.0, Duration = 100
                        }
                    }
                }
                
                
            };
            Rans = new ObservableCollection<CharacteristicRAN>()
            {
                new()
                {
                    Board = CharacteristicRAN.Boards.L,
                    Duration = 3,
                    MinSignal = 500,
                    MaxSignal = 1010,
                    N = 5
                },
                new()
                {
                    Board = CharacteristicRAN.Boards.L,
                    Duration = 3,
                    MinSignal = 990,
                    MaxSignal = 2010,
                    N = 5
                },
                new()
                {
                    Board = CharacteristicRAN.Boards.L,
                    Duration = 4,
                    MinSignal = 1990,
                    MaxSignal = 4000,
                    N = 5
                },
                new()
                {
                    Board = CharacteristicRAN.Boards.R,
                    Duration = 4,
                    MinSignal = 590,
                    MaxSignal = 1010,
                    N = 5
                },
                new()
                {
                    Board = CharacteristicRAN.Boards.R,
                    Duration = 3,
                    MinSignal = 990,
                    MaxSignal = 2010,
                    N = 5
                },
                new()
                {
                    Board = CharacteristicRAN.Boards.R,
                    Duration = 3,
                    MinSignal = 1990,
                    MaxSignal = 4000,
                    N = 5
                }
            };


            BuildModelCommand = new RelayCommand(OnBuildModelCommand);
            Modeling = new RelayCommand(OnModelingCommand);
            EditIRICommand = new RelayCommand(OnEditIRI, OnSelectedIRICheck);
            SaveIRICommand = new RelayCommand(OnSaveIRI);
            RemoveIRICommand = new RelayCommand(OnRemoveIRI, OnSelectedIRICheck);
            AddIRICommand = new RelayCommand(AddIRI);
        }


        #region Private methods

        private void AddIRI(object obj)
        {
            SelectedIRI = new IRI()
            {
                CharacteristicIri = new CharacteristicIRI(), IriStream = new List<CharacteristicStream>()
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

        private void SetNumber(ObservableCollection<IRI> iris)
        {
            foreach (var iri in iris)
            {
                iri.Number = iris.IndexOf(iri) + 1;
            }
        }


        public Visibility ModelingButtonVisibility { get; set; } = Visibility.Visible;
        public Visibility ProgressVisibility { get; set; } = Visibility.Collapsed;
        private object locker = new object();
        private void OnModelingCommand(object Obj)
        {
            
            
            foreach (var ran in Rans)
            {
                ran.N = Npak;
                ran.BMin = Bmin;
                ran.BMax = Bmax;
            }
            Task.Run(() =>
            {
                ModelingButtonVisibility = Visibility.Collapsed;
                ProgressVisibility = Visibility.Visible;
                
                List<(List<Package> all_data, List<List<Package>> packages)> results = new List<(List<Package> all_data, List<List<Package>> packages)>();
                SetNumber(Iris);
                Parallel.ForEach(Iris, iri =>
                {
                    var res = Calc.MakeStream(
                        iri.CharacteristicIri,
                        iri.IriStream,
                        new CharacteristicMovingLA()
                        {
                            Coords = Coords.ToList(), Height = Height, Speed = Speed, Time = Time
                        }, Rans.ToList(), new Catalog());
                    
                    foreach (var package in res.all_data)
                    {
                        package.Number = iri.Number;
                        package.Type = iri.CharacteristicIri.Type;
                    }
                    foreach (var package_arr in res.packages)
                    {
                        foreach (var package in package_arr)
                        {
                            package.Number = iri.Number;
                            package.Type = iri.CharacteristicIri.Type;
                        }
                    }
                    
                    lock (locker)
                    {
                        results.Add(res);
                    }
                });

                var completeArrays = Calc.RanUnion(results, 
                    TimeSigma / 1000000, 
                    CoordSigma, 
                    HeightSigma, 
                    PsiSigma, 
                    CSigma, 
                    FSigma, 
                    TauSigma);

                Arr1 = new ObservableCollection<Package>(completeArrays.arr1);
                Arr2 = new ObservableCollection<Package>(completeArrays.arr2);
                Arr3 = new ObservableCollection<Package>(completeArrays.arr3);
                Arr4 = new ObservableCollection<Package>(completeArrays.arr4);
                
                File.WriteAllText("../arr1.txt", string.Join('\n', Arr1));
                File.WriteAllText("../arr2.txt", string.Join('\n', Arr2));
                File.WriteAllText("../arr3.txt", string.Join('\n', Arr3));
                File.WriteAllText("../arr4.txt", string.Join('\n', Arr4));
                
                ModelingButtonVisibility = Visibility.Visible;
                ProgressVisibility = Visibility.Collapsed;
            });
        }

        public ObservableCollection<Package> Arr1 { get; set; }
        public ObservableCollection<Package> Arr2 { get; set; }
        public ObservableCollection<Package> Arr3 { get; set; }
        public ObservableCollection<Package> Arr4 { get; set; }

        private void OnBuildModelCommand(object Obj)
        {

        }

        #endregion

    }
}