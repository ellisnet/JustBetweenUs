using System;
using System.Threading.Tasks;
using Uno.UI.Hosting;

// ReSharper disable CheckNamespace

namespace JustBetweenUs;

internal class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWin32()
            .Build();

        await host.RunAsync();
    }

    //[STAThread]
    //public static void Main(string[] args)
    //{
    //    App.InitializeLogging();

    //    var host = UnoPlatformHostBuilder.Create()
    //        .App(() => new App())
    //        .UseWin32()
    //        .Build();

    //    host.Run();
    //}
}
