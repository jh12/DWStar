using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DAT10.Core;
using DAT10.Core.Setting;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;
using DAT10.StarModelComponents;
using DAT10Gui.View.Controls.CommonModelViewer;
using DAT10Gui.View.Controls.StarModelViewer;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;
using MaterialDesignThemes.Wpf;
using SimpleLogger;

namespace DAT10Gui.ViewModel
{
    public class ExecutionViewModel : ViewModelBase
    {
        private readonly ModuleEngine _moduleEngine;
        private readonly DataSampleService _sampleService;

        public List<Configuration> Configurations { get; set; }
        public Configuration CurrConfiguration { get; set; }

        public ObservableCollection<CommonGraph> CommonGraphs { get; private set; } = new ObservableCollection<CommonGraph>();
        public ObservableCollection<StarGraph> StarGraphs { get; private set; } = new ObservableCollection<StarGraph>();

        public RelayCommand ExecuteConfiguration { get; private set; }
        public RelayCommand NextStep { get; }
        public RelayCommand<CommonGraph> ShowCommonModel { get; private set; }
        public RelayCommand<CommonGraph> RemoveCommonModel { get; private set; }
        public RelayCommand<StarGraph> ShowStarModel { get; private set; }
        public RelayCommand<StarGraph> RemoveStarModel { get; private set; }

        public int CurrentPhase { get; private set; }

        private float THRESHOLD = 0.8f;

        public ExecutionViewModel(ModuleEngine moduleEngine, DataSampleService sampleService)
        {
            _moduleEngine = moduleEngine;
            _sampleService = sampleService;

            Configurations = moduleEngine.Configurations;
            CurrConfiguration = moduleEngine.CurrentConfiguration;

            ExecuteConfiguration = new RelayCommand(OnExecuteConfiguration, () => !_isExecuting);
            NextStep = new RelayCommand(OnNextStep, () => !_isExecuting);
            ShowCommonModel = new RelayCommand<CommonGraph>(OnShowCommonModel);
            RemoveCommonModel = new RelayCommand<CommonGraph>(OnRemoveCommonModel);
            ShowStarModel = new RelayCommand<StarGraph>(OnShowStarModel);
            RemoveStarModel = new RelayCommand<StarGraph>(OnRemoveStarModel);
        }



        private bool _isExecuting;

        public bool IsExecuting
        {
            get { return _isExecuting; }
            set
            {
                _isExecuting = value;
                RaisePropertyChanged();
                ExecuteConfiguration.RaiseCanExecuteChanged();
                NextStep.RaiseCanExecuteChanged();
            }
        }

        private bool _isStepping;
        public bool IsStepping
        {
            get { return _isStepping;}
            set
            {
                _isStepping = value;
                RaisePropertyChanged();
            }
        }

        #region Commands

        /// <summary>
        /// Run all steps end-to-end
        /// </summary>
        private async void OnExecuteConfiguration()
        {
            IsExecuting = true;
            IsStepping = true;

            stopwatch.Restart();

            while (IsStepping)
            {
                await RunNextStep();
            }

            stopwatch.Stop();
            Logger.Log(Logger.Level.Severe, $"Total: {stopwatch.Elapsed}");

            IsExecuting = false;
        }

        private double _phaseProgress;

        public double PhaseProgress
        {
            get { return _phaseProgress; }
            set
            {
                _phaseProgress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Run one step at a time
        /// </summary>
        private async void OnNextStep()
        {
            IsExecuting = true;

            await RunNextStep();

            IsExecuting = false;
        }

        private Stopwatch stopwatch = new Stopwatch();
        private Stopwatch phaseStopwatch = new Stopwatch();

        /// <summary>
        /// Process next available step.
        /// Resets when starting from the beginning
        /// </summary>
        private async Task RunNextStep()
        {
            // Reset if limit has been reached
            if (CurrentPhase > 6)
            {
                _intermediaryCommonModels = new List<CommonModel>();
                _intermediaryStarModels = new List<StarModel>();
                _sampleService.ResetService();

                CommonGraphs.Clear();
                StarGraphs.Clear();

                CurrentPhase = 0;
            }

            PhaseProgress = (CurrentPhase + 1) / 7d;
            phaseStopwatch.Restart();
            switch (CurrentPhase)
            {
                case 0: // Run metadata phase
                    IsStepping = true;
                    await Task.Run(ExecuteMetadataPhase);
                    break;
                case 1: // Run refinement phase
                    await Task.Run(ExecuteRefinementPhase);

                    if (CurrConfiguration.CombinationModule.Count == 0)
                        CurrentPhase++;

                    break;
                case 2: // Run star phase
                    await Task.Run(ExecuteStarCombinePhase);
                    break;
                case 3: // Run star fact phase
                    await Task.Run(ExecuteStarFactPhase);
                    break;
                case 4: // Run star dimension phase
                    await Task.Run(ExecuteStarDimensionPhase);
                    break;
                case 5: // Run star refinement phase
                    await Task.Run(ExecuteStarRefinementPhase);
                    break;
                case 6: // Run generation phase
                    await Task.Run(ExecuteGenerationPhase);
                    IsStepping = false;
                    break;
                default:
                    IsStepping = false;
                    break;
            }
            phaseStopwatch.Stop();
            Logger.Log(Logger.Level.Severe, $"Phase {CurrentPhase}: {phaseStopwatch.Elapsed}");

            CurrentPhase++;
        }

        private async void OnShowCommonModel(CommonGraph commonModel)
        {
            CommonModelViewer viewer = new CommonModelViewer
            {
                Graph = commonModel,
                Margin = new Thickness(5),
                MaxWidth = 600,
                MaxHeight = 600,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            await DialogHost.Show(viewer, "RootDialog");
        }

        private void OnRemoveCommonModel(CommonGraph commonModel)
        {
            CommonGraphs.Remove(commonModel);
            _intermediaryCommonModels.Remove(commonModel.BasedOn);
        }

        private async void OnShowStarModel(StarGraph starModel)
        {
            StarModelViewer viewer = new StarModelViewer
            {
                Graph = starModel,
                Margin = new Thickness(5),
                MaxWidth = 600,
                MaxHeight = 600,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            await DialogHost.Show(viewer, "RootDialog");
        }

        private void OnRemoveStarModel(StarGraph starModel)
        {
            StarGraphs.Remove(starModel);
            _intermediaryStarModels.Remove(starModel.BasedOn);
        }

        #endregion

        private List<CommonModel> _intermediaryCommonModels;
        private List<StarModel> _intermediaryStarModels;

        private async Task ExecuteMetadataPhase()
        {
            // Set current configuration according to the selected configuration
            _moduleEngine.CurrentConfiguration = CurrConfiguration;
            _intermediaryCommonModels = new List<CommonModel> { _moduleEngine.GetDatabases()};

            // Show common models as graph
            await DispatcherHelper.RunAsync(() =>
            {
                CommonGraphs = new ObservableCollection<CommonGraph>(_intermediaryCommonModels.Select(cm => new CommonGraph(cm)));
                RaisePropertyChanged(nameof(CommonGraphs));
            });
        }

        private async Task ExecuteRefinementPhase()
        {
            // If no databases then just ignore the following
            if (_intermediaryCommonModels.Count == 0)
                return;

            // Refinement
            _intermediaryCommonModels = await _moduleEngine.RefineDatabases(_intermediaryCommonModels);

            // Group common models
            _intermediaryCommonModels = _moduleEngine.GroupCommonModels(_intermediaryCommonModels);

            // Show common models as graph
            await DispatcherHelper.RunAsync(() =>
            {
                CommonGraphs = new ObservableCollection<CommonGraph>(_intermediaryCommonModels.Select(cm => new CommonGraph(cm)));
                RaisePropertyChanged(nameof(CommonGraphs));
            });
        }

        private async Task ExecuteStarCombinePhase()
        {
            // Combined common models
            List<CommonModel> combinedCommonModels = new List<CommonModel>();
            foreach (CommonModel groupedCommonModel in _intermediaryCommonModels)
            {
                combinedCommonModels.AddRange(_moduleEngine.GenerateCombinedCommonModels(groupedCommonModel));
            }

            _intermediaryCommonModels = combinedCommonModels;

            // Show common models as graph
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                CommonGraphs = new ObservableCollection<CommonGraph>(combinedCommonModels.Select(cm => new CommonGraph(cm)));
                RaisePropertyChanged(nameof(CommonGraphs));
            });
        }

        private async Task ExecuteStarFactPhase()
        {
            // Find fact tables
            List<StarModel> multidims = new List<StarModel>();
            foreach (var commonModel in _intermediaryCommonModels)
            {
                //TODO: har udkommenteret await
                //var multidims = await _moduleEngine.GenerateStarModels(databases);
                multidims.AddRange(_moduleEngine.GenerateStarModels(commonModel, THRESHOLD));
            }

            _intermediaryStarModels = multidims;

            await DispatcherHelper.RunAsync(() =>
            {
                StarGraphs = new ObservableCollection<StarGraph>(multidims.Select(starModel => new StarGraph(starModel)));
                RaisePropertyChanged(nameof(StarGraphs));
            });
        }

        private async Task ExecuteStarDimensionPhase()
        {
            // Find dimensions to fact tables
            _intermediaryStarModels = _moduleEngine.GenerateStarModelsAfterFact(_intermediaryStarModels);

            // Show star models as graph
            await DispatcherHelper.RunAsync(() =>
            {
                StarGraphs = new ObservableCollection<StarGraph>(_intermediaryStarModels.Select(starModel => new StarGraph(starModel)));
                RaisePropertyChanged(nameof(StarGraphs));
            });
        }

        private async Task ExecuteStarRefinementPhase()
        {
            _intermediaryStarModels = _moduleEngine.RefineStarModels(_intermediaryStarModels);

            // Show refined star models as graph
            await DispatcherHelper.RunAsync(() =>
            {
                StarGraphs = new ObservableCollection<StarGraph>(_intermediaryStarModels.Select(starModel => new StarGraph(starModel)));
                RaisePropertyChanged(nameof(StarGraphs));
            });
        }

        private async Task ExecuteGenerationPhase()
        {
            _moduleEngine.GenerateModels(_intermediaryStarModels);
        }
    }
}
