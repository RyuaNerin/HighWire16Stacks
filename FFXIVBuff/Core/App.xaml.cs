using System.Windows;

namespace FFXIVBuff.Core
{
    internal partial class App : Application
    {
        public const string Name = "FFIXV-StatusOverlay";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Sentry.Load();

            Settings.Load();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Instance.Save();
        }
    }
}
