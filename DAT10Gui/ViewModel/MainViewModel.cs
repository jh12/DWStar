using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Documents;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using SimpleInjector;
using SimpleLogger;
using SimpleLogger.Logging;

namespace DAT10Gui.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public IList<HamburgerMenuGlyphItem> MenuItems { get; set; }
        private Container _container;
        public ISnackbarMessageQueue SnackbarMessageQueue { get; private set; }

        public int SelectedIndex { get; set; }

        private ViewModelBase _currViewModel;
        public ViewModelBase CurrViewModel
        {
            get
            {
                return _currViewModel;
            }
            private set
            {
                _currViewModel = value;
                SelectedIndex = MenuItems.IndexOf(MenuItems.First(i => i.TargetPageType == value.GetType()));
                RaisePropertyChanged(nameof(SelectedIndex));
                RaisePropertyChanged();
            }
        }

        public MainViewModel(Container container, ISnackbarMessageQueue snackbarQueue)
        {
            MenuItems = new List<HamburgerMenuGlyphItem>
            {
                new HamburgerMenuGlyphItem {Glyph = PackIconKind.Home.ToString(), Label = "Home", TargetPageType = typeof(HomeViewModel)},
                new HamburgerMenuGlyphItem {Glyph = PackIconKind.LanConnect.ToString(), Label = "Connections", TargetPageType = typeof(ConnectionViewModel)},
                new HamburgerMenuGlyphItem {Glyph = PackIconKind.Run.ToString(), Label = "Execute", TargetPageType = typeof(ExecutionViewModel)},
                new HamburgerMenuGlyphItem {Glyph = PackIconKind.CubeUnfolded.ToString(), Label = "Configuration", TargetPageType = typeof(ConfigurationViewModel)}, // Collage, CubeUnfolded, Puzzle
                new HamburgerMenuGlyphItem {Glyph = PackIconKind.FormatAlignLeft.ToString(), Label = "Log", TargetPageType = typeof(LogViewModel)}
            };

            _container = container;
            SnackbarMessageQueue = snackbarQueue;

            CurrViewModel = container.GetInstance<ExecutionViewModel>();

            SetupListeners();

            SubscribableHandler.MessageStream.Where(msg => msg.Level == Logger.Level.Error).Subscribe(OnError);

            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        private void OnError(LogMessage logMessage)
        {
            SnackbarMessageQueue.Enqueue("An error has occured.", "View error", () =>
            {
                CurrViewModel = _container.GetInstance<LogViewModel>();
            }, true);
        }

        /// <summary>
        /// Setup messaging and listen for messages
        /// </summary>
        private void SetupListeners()
        {
            // Register for messages related to switching view in hamburger menu
            MessengerInstance.Register<Type>(this, "SwitchHamburgerContext", type =>
            {
                CurrViewModel = (ViewModelBase) _container.GetInstance(type);
            });
        }
    }
}