using StoreManager.Classes;
using System.Windows;

namespace StoreManager
{
    public partial class App : Application
    {
        protected async void Application_Startup(object sender, StartupEventArgs e)
        {
            await Database.Initialize();
        }

    }
}
