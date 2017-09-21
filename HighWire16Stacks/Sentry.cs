using System;
using System.Windows.Forms;
using System.Windows.Threading;
using SharpRaven;
using SharpRaven.Data;
using System.Diagnostics;

namespace HighWire16Stacks
{
    internal static class Sentry
    {
        private static readonly RavenClient ravenClient;

        static Sentry()
        {
            ravenClient = new RavenClient("https://c25491925dff4a53a3116387b89f0788:13a21e4bf01d421e82e645f70ed1e388@sentry.io/143322")
            {
                Environment = Application.ProductName,
                Logger      = Application.ProductName,
                Release     = Application.ProductVersion
            };
            System.AppDomain.CurrentDomain.UnhandledException                += (s, e) => HandleException(e.ExceptionObject as Exception);
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException     += (s, e) => HandleException(e.Exception);
            System.Windows.Forms.Application.ThreadException                 += (s, e) => HandleException(e.Exception);

            AddHandler(System.Windows.Application.Current.Dispatcher);
        }

        public static void Load()
        {
        }

        public static void AddHandler(Dispatcher dispatcher)
        {
            dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }
        public static void RemoveHandler(Dispatcher dispatcher)
        {
            dispatcher.UnhandledException -= Dispatcher_UnhandledException;
        }

        private static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        public static void HandleException(Exception ex)
        {
            if (ex == null)
                return;

            Error(ex, null);
        }

        public static void Info(object data, string format, params object[] args)
        {
            SentryEvent ev = new SentryEvent(new SentryMessage(format, args))
            {
                Level = ErrorLevel.Info,
                Extra = data
            };
            Report(ev);
        }
        
        public static void Error(Exception ex)
        {
            Error(ex, null);
        }
        public static void Error(Exception ex, object data)
        {
#if DEBUG
            Debug.Assert(false, ex.ToString());
#else
            var ev = new SentryEvent(ex);
            ev.Level = ErrorLevel.Error;
            ev.Extra = data;

            Report(ev);
#endif
        }
        
        private static void Report(SentryEvent @event)
        {
            @event.Tags.Add("ARCH", Environment.Is64BitOperatingSystem ? "x64" : "x86");
            @event.Tags.Add("OS", Environment.OSVersion.VersionString);
            @event.Tags.Add("NET", Environment.Version.ToString());

            ravenClient.CaptureAsync(@event);
        }
    }
}
