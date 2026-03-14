using JustBetweenUs.Encryption;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

// ReSharper disable RedundantExtendsListEntry

namespace JustBetweenUs.Mobile;

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
        //MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        return new Window(new AppShell());
    }
}
