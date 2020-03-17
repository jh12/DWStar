using System;
using System.Windows;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Threading;
using SimpleLogger;

namespace DAT10Gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();

            Logger.LoggerHandlerManager.AddHandler(new SubscribableHandler());
        }

        public App()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += ExceptionLogger;
        }

        private void ExceptionLogger(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception) e.ExceptionObject;
        }
    }
}
