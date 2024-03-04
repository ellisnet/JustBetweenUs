using JustBetweenUs.Encryption;
using System.Windows;

namespace JustBetweenUs;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        SimpleServiceResolver.CreateInstance(services =>
        {
            //Register my custom services here
            services.AddEncryption();
        });
        SimpleViewModel.SetIsDesignMode(false);
    }
}
