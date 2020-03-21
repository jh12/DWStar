using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using SimpleLogger;
using SimpleLogger.Logging;
using SimpleLogger.Logging.Formatters;

namespace DAT10Gui
{
    public class SubscribableHandler : ILoggerHandler
    {
        private readonly ILoggerFormatter _loggerFormatter;

        static SubscribableHandler()
        {
            LogMessages = new Subject<LogMessage>();
            MessageStream = LogMessages;
        }

        public SubscribableHandler() : this(new GUILoggerFormatter())
        { }

        public SubscribableHandler(ILoggerFormatter loggerFormatter)
        {
            _loggerFormatter = loggerFormatter;
        }

        public void Publish(LogMessage logMessage)
        {
            LogMessages.OnNext(logMessage);
        }

        private static readonly Subject<LogMessage> LogMessages;
        public static readonly IObservable<LogMessage> MessageStream;

        //public IObservable<LogMessage> LogMessages()
        //{
        //    return Observable.Create<LogMessage>(observer =>
        //    {


        //        return Disposable.Create(() => Logger.Debug.Log("Object unsubscribed from log."));
        //    });
        //}
    }

    public class GUILoggerFormatter : ILoggerFormatter
    {
        public string ApplyFormat(LogMessage logMessage)
        {
            return $"{logMessage.DateTime:HH:mm:ss}: {logMessage.Level} [line: {logMessage.LineNumber} {logMessage.CallingClass} -> {logMessage.CallingMethod}()]: {logMessage.Text}";
        }
    }
}