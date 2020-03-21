using System;
using DAT10.Core;
using DAT10.Modules.Inference;
using DAT10Gui.ViewModel.Misc;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using MaterialDesignThemes.Wpf;
using Microsoft.Practices.ServiceLocation;
using SimpleInjector;
using SimpleLogger;

namespace DAT10Gui.ViewModel
{
    public class ViewModelLocator
    {
        private Container _container;

        public ViewModelLocator()
        {
            _container = new Container();

            var locatorAdapter = new SimpleInjectorServiceLocatorAdapter(_container);
            ServiceLocator.SetLocatorProvider(() => locatorAdapter);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}
            
            _container.Register<Container>(() => _container, Lifestyle.Singleton);
            _container.Register<ISnackbarMessageQueue>(() => new SnackbarMessageQueue(), Lifestyle.Singleton);
            _container.Register<ViewModelLocator>(() => this, Lifestyle.Singleton);

            // Windows
            _container.Register<MainViewModel>(Lifestyle.Singleton);

            // Controls
            _container.Register<HomeViewModel>();
            _container.Register<ConnectionViewModel>(Lifestyle.Singleton);
            _container.Register<ExecutionViewModel>(Lifestyle.Singleton);
            _container.Register<ConfigurationViewModel>(Lifestyle.Singleton);
            _container.Register<LogViewModel>(Lifestyle.Singleton);

            SettingsService settingsService = new SettingsService();

            _container.Register<DataSampleService>(Lifestyle.Singleton);
            _container.Register<SettingsService>(() => settingsService, Lifestyle.Singleton);

            _container.Register<ModuleEngine>(() => ModuleEngine.CreateInstance(_container, settingsService).Result, Lifestyle.Singleton);

            try
            {
                _container.Verify();
            }
            catch (Exception e)
            {
                Logger.Log(e);
                throw;
            }

        }

        public MainViewModel Main => _container.GetInstance<MainViewModel>();

        public HomeViewModel Home => _container.GetInstance<HomeViewModel>();
        public ConnectionViewModel Connection => _container.GetInstance<ConnectionViewModel>();
        public ExecutionViewModel Execution => _container.GetInstance<ExecutionViewModel>();
        public ConfigurationViewModel Configuration => _container.GetInstance<ConfigurationViewModel>();
        public LogViewModel Log => _container.GetInstance<LogViewModel>();
        
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}