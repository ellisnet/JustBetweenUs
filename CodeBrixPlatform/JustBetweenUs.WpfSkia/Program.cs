using System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf;

// ReSharper disable CheckNamespace

namespace JustBetweenUs;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWindows()
            .Build();

        // The WPF host's default OpenGL renderer draws via raw opengl32 onto WPF's own
        // DirectX-composited HWND, which causes "airspace" conflicts on many systems —
        // the window shows but the Uno content never composites (blank black/white window).
        // Software rendering blits the Skia frame into WPF and composites correctly.
        if (host is WpfHost wpfHost)
        {
            wpfHost.RenderSurfaceType = RenderSurfaceType.Software;
        }

        host.Run();
    }
}
