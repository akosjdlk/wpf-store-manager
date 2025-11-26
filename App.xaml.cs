using System;
using System.Threading.Tasks;
using System.Windows;
using StoreManager.Classes;

namespace StoreManager
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            await Database.Initialize();
            base.OnStartup(e);
        }
    }

}
