using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Reflection;

namespace Openza.Reader.Avalonia;

public sealed partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "Development build" : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    private void OnDocumentationClicked(object? sender, RoutedEventArgs e) => OpenUri("https://solanky.dev/openza/reader/");

    private void OnSourceClicked(object? sender, RoutedEventArgs e) => OpenUri("https://github.com/openza/reader");

    private void OnIssuesClicked(object? sender, RoutedEventArgs e) => OpenUri("https://github.com/openza/reader/issues");

    private void OnSecurityClicked(object? sender, RoutedEventArgs e) => OpenUri("https://github.com/openza/reader/blob/main/SECURITY.md");

    private static void OpenUri(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
