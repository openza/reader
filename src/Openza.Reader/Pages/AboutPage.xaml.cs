using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using Windows.ApplicationModel;

namespace Openza.Reader.Pages;

public sealed partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"Version {GetVersion()}";
    }

    private static string GetVersion()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch (InvalidOperationException)
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        }
    }
}
