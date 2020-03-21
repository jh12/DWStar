using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using SimpleLogger;
using SimpleLogger.Logging;
using SimpleLogger.Logging.Formatters;

namespace DAT10Gui.ViewModel
{
    public class LogViewModel : ViewModelBase
    {
        public ObservableCollection<LogGuiMessage> LogMessages { get; set; }

        public LogViewModel()
        {
            LogMessages = new ObservableCollection<LogGuiMessage>();

            SubscribableHandler.MessageStream.Subscribe(message =>
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() => LogMessages.Add(new LogGuiMessage(message)));
            });
        }

        public class LogGuiMessage
        {
            public string Time { get; }
            public string Text { get; }
            public string Location { get; }
            public Brush Color { get; }

            public LogGuiMessage(LogMessage message)
            {
                Time = $"{message.DateTime:HH:mm:ss}";
                Location = $" [line: {message.LineNumber} {message.CallingClass} -> {message.CallingMethod}()]";
                Text = $"{message.Level}: {message.Text}";

                switch (message.Level)
                {
                    case Logger.Level.Error:
                    case Logger.Level.Severe:
                        Color = Brushes.Red;
                        break;
                    case Logger.Level.Warning:
                        Color = Brushes.DarkOrange;
                        break;
                    case Logger.Level.Debug:
                        Color = Brushes.DarkSlateGray;
                        break;
                    default:
                        Color = Brushes.Black;
                        break;
                }
            }
        }
    }
}
