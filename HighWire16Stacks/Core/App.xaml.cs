using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Windows;

namespace HighWire16Stacks.Core
{
    internal partial class App : Application
    {
        private static readonly string exeLocation;
        public static string ExeLocation => exeLocation;

        static App()
        {
            exeLocation = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);

            WebRequest.DefaultCachePolicy = new RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            WebRequest.DefaultWebProxy = null;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Sentry.Load();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Instance?.Save();
        }
    }
}
