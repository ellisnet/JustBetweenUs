using JustBetweenUs.Encryption;
using Microsoft.Maui.Controls;

namespace JustBetweenUs.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SimpleServiceResolver.CreateInstance(services =>
            {
                //Register my custom services here
                services.AddEncryption();
            });
            SimpleViewModel.SetIsDesignMode(false);
            MainPage = new AppShell();
        }
    }
}
