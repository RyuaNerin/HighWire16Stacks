using System.Windows;

namespace FFXIVBuff.Core
{
    internal partial class App : Application
    {
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
