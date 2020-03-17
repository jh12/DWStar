using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DAT10.Core;
using DAT10.Core.Setting;
using DAT10.Modules;
using DAT10.Modules.CombinationPhase;
using DAT10.Modules.Dimensions;
using DAT10.Modules.Generation;
using DAT10.Modules.Inference;
using DAT10.Modules.Multidimensional;
using DAT10.Modules.Refinement;
using DAT10.Modules.StarRefinement;
using DAT10Gui.Annotations;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace DAT10Gui.ViewModel
{
    public class ConfigurationViewModel : ViewModelBase
    {
        private readonly ModuleEngine _moduleEngine;

        public List<ConfigurationCategory> Categories { get; set; }

        public ObservableCollection<Configuration> Configurations { get; set; }

        public Configuration CurrConfiguration
        {
            get { return _currConfiguration; }
            set
            {
                if (_currConfiguration == value)
                    return;

                _currConfiguration = value;
                LoadConfiguration();
                RaisePropertyChanged();
            }
        }

        public ConfigurationViewModel(ModuleEngine moduleEngine)
        {
            _moduleEngine = moduleEngine;

            Categories = new List<ConfigurationCategory>
            {
                new ConfigurationCategory("Metadata phase"),
                new ConfigurationCategory("Refinement phase"),
                new LimitedConfigurationCategory("Star phase (Combination)", 1),
                new ConfigurationCategory("Star phase (Find fact tables)"),
                new ConfigurationCategory("Star phase (Find dimensions)"),
                new ConfigurationCategory("Star refinement phase"),
                new ConfigurationCategory("Generation phase")
            };


            // Configurations
            Configurations = new ObservableCollection<Configuration>(moduleEngine.Configurations);
            CurrConfiguration = moduleEngine.CurrentConfiguration;

            CreateConfiguration = new RelayCommand(OnCreateConfiguration);
            SaveConfiguration = new RelayCommand(OnSaveConfiguration, () => !_isSaving);
        }

        private void LoadConfiguration()
        {
            // Inference modules
            var allInferenceModules = _moduleEngine.SchemaInferenceModules;
            var activeInferenceModules = CurrConfiguration.InferenceModules;

            var schemaInferenceModules = allInferenceModules.Except(activeInferenceModules);
            var selectedSchemaInferenceModules = activeInferenceModules;

            // Refinement modules
            var allRefinementModules = _moduleEngine.RefinementModules;
            var activeRefinementModules = CurrConfiguration.RefinementModules;

            var refinementModules = allRefinementModules.Except(activeRefinementModules);
            var selectedRefinementModules = activeRefinementModules;

            // Combination modules
            var allCombinationModules = _moduleEngine.CombinationModule;
            var activeCombinationModules = CurrConfiguration.CombinationModule;

            var combinationModules = allCombinationModules.Except(activeCombinationModules);
            var selectedCombinationModules = activeCombinationModules;

            // Fact modules
            var allFactModules = _moduleEngine.MultidimModules;
            var activeFactModules = CurrConfiguration.MultidimFactModules;

            var factModules = new ObservableCollection<IModule>(allFactModules.Except(activeFactModules));
            var selectedFactModules = new ObservableCollection<IModule>(activeFactModules);

            // Dimension modules
            var allDimensionModules = _moduleEngine.DimensionalModules;
            var activeDimensionModules = CurrConfiguration.DimensionModules;

            var dimensionModules = new ObservableCollection<IModule>(allDimensionModules.Except(activeDimensionModules));
            var selectedDimensionModules = new ObservableCollection<IModule>(activeDimensionModules);

            // Star refinement modules
            var allStarRefinementModules = _moduleEngine.StarRefinementModules;
            var activeStarRefinementModules = CurrConfiguration.StarRefinementModules;

            var starRefinementModules = new ObservableCollection<IModule>(allStarRefinementModules.Except(activeStarRefinementModules));
            var selectedStarRefinementModules = new ObservableCollection<IModule>(activeStarRefinementModules);

            // Generation modules
            var allGenerationModules = _moduleEngine.GenerationModules;
            var activeGenerationModules = CurrConfiguration.GenerationModules;

            var generationModules = new ObservableCollection<IModule>(allGenerationModules.Except(activeGenerationModules));
            var selectedGenerationModules = new ObservableCollection<IModule>(activeGenerationModules);

            // Set metadata modules
            Categories[0].SetModules(schemaInferenceModules, selectedSchemaInferenceModules);
            // Set refinement modules
            Categories[1].SetModules(refinementModules, selectedRefinementModules, _moduleEngine.RefinementModules.Cast<IDependent>().ToList());
            // Set combination modules
            Categories[2].SetModules(combinationModules, selectedCombinationModules);
            // Set fact modules
            Categories[3].SetModules(factModules, selectedFactModules);
            // Set dimension modules
            Categories[4].SetModules(dimensionModules, selectedDimensionModules, _moduleEngine.DimensionalModules.Cast<IDependent>().ToList());
            // Set star refinement modules
            Categories[5].SetModules(starRefinementModules, selectedStarRefinementModules);
            // Set generation modules
            Categories[6].SetModules(generationModules, selectedGenerationModules);
        }

        #region Configuration changes

        public RelayCommand CreateConfiguration { get; private set; }

        private void OnCreateConfiguration()
        {
            var configuration = new Configuration();
            string tempName = "New config";

            int duplicates = _moduleEngine.Configurations.Count(c => c.Name.StartsWith(tempName));

            if (duplicates > 0)
                tempName = $"{tempName} ({duplicates})";

            configuration.Name = tempName;

            _moduleEngine.Configurations.Add(configuration);
            Configurations.Add(configuration);
            CurrConfiguration = configuration;
        }

        #endregion

        #region Save

        public RelayCommand SaveConfiguration { get; set; }
        private bool _isSaving;
        private Configuration _currConfiguration;

        private async void OnSaveConfiguration()
        {
            CurrConfiguration.InferenceModules = Categories[0].SelectedModules.Cast<InferenceModuleBase>().ToList();
            CurrConfiguration.RefinementModules = Categories[1].SelectedModules.Cast<RefinementModuleBase>().ToList();
            CurrConfiguration.CombinationModule = Categories[2].SelectedModules.Cast<ICombinationModule>().ToList();
            CurrConfiguration.MultidimFactModules = Categories[3].SelectedModules.Cast<IMultidimModuleFact>().ToList();
            CurrConfiguration.DimensionModules = Categories[4].SelectedModules.Cast<DimensionalModuleBase>().ToList();
            CurrConfiguration.StarRefinementModules = Categories[5].SelectedModules.Cast<IStarRefinement>().ToList();
            CurrConfiguration.GenerationModules = Categories[6].SelectedModules.Cast<IGeneration>().ToList();

            _isSaving = true;
            SaveConfiguration.RaiseCanExecuteChanged();

            await _moduleEngine.Save();

            _isSaving = false;
            SaveConfiguration.RaiseCanExecuteChanged();
        }
        #endregion

    }

    public class ConfigurationCategory : INotifyPropertyChanged
    {
        public string Name { get; set; }

        public ObservableCollection<IModule> AvailableModules { get; set; }
        public ObservableCollection<IModule> SelectedModules { get; set; }
        public List<IDependent> Ordering { get; set; }

        public ConfigurationCategory(string name)
        {
            Name = name;
            AvailableModules = new ObservableCollection<IModule>();
            SelectedModules = new ObservableCollection<IModule>();
        }

        public void SetModules(IEnumerable<IModule> availableModules, IEnumerable<IModule> selectedModules, List<IDependent> ordering = null)
        {
            AvailableModules.Clear();
            foreach (var module in availableModules)
            {
                AvailableModules.Add(module);
            }

            SelectedModules.Clear();
            foreach (var module in selectedModules)
            {
                SelectedModules.Add(module);
            }

            Ordering = ordering;
            RaisePropertyChanged(nameof(Ordering));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LimitedConfigurationCategory : ConfigurationCategory
    {
        public int Limit { get; }

        public LimitedConfigurationCategory(string name, int limit) : base(name)
        {
            Limit = limit;
        }
    }
}
