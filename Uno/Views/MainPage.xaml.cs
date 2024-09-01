using JustBetweenUs.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace JustBetweenUs.Uno.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        //Doing this before InitializeComponent() - in case InitializeComponent()
        //  is the thing that sets the data context.
        DataContextChanged += (sender, args) =>
        {
            (DataContext as IXamlRootGetter)?.SetXamlRootGetter(() => XamlRoot);

            if (DataContext is ICopyToClipboard copy)
            {
                copy.CopyTextToClipboard = (text) =>
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        var clipData = new DataPackage();
                        clipData.SetText(text);
                        Clipboard.SetContent(clipData);
                    }
                };
            }
        };

        InitializeComponent();
    }
}
