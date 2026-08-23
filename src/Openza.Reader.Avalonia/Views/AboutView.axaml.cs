using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Reflection;

namespace Openza.Reader.Avalonia.Views;

public sealed partial class AboutView : UserControl
{
    private const double NarrowWidth = 760;

    public event Action<object?, string>? LinkRequested;

    public AboutView()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "Development build" : $"Version {version.Major}.{version.Minor}.{version.Build}";
    }

    private void OnDocumentationClicked(object? sender, RoutedEventArgs e) => Request("https://solanky.dev/openza/reader/");

    private void OnSourceClicked(object? sender, RoutedEventArgs e) => Request("https://github.com/openza/reader");

    private void OnIssuesClicked(object? sender, RoutedEventArgs e) => Request("https://github.com/openza/reader/issues");

    private void OnSecurityClicked(object? sender, RoutedEventArgs e) => Request("https://github.com/openza/reader/blob/main/SECURITY.md");

    private void OnThirdPartyNoticesClicked(object? sender, RoutedEventArgs e) => Request("https://github.com/openza/reader/blob/main/THIRD-PARTY-NOTICES.md");

    private void Request(string uri) => LinkRequested?.Invoke(this, uri);

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < NarrowWidth;
        AboutScrollViewer.Padding = narrow ? new Thickness(24, 0, 24, 36) : new Thickness(106, 0, 36, 36);
        AboutContent.MaxWidth = narrow ? double.PositiveInfinity : 1064;
        LinksGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        LinksGrid.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        PositionLink(DocumentationButton, narrow ? 0 : 0, 0);
        PositionLink(SourceButton, narrow ? 1 : 0, narrow ? 0 : 1);
        PositionLink(IssuesButton, narrow ? 2 : 1, 0);
        PositionLink(SecurityButton, narrow ? 3 : 1, narrow ? 0 : 1);
        PositionLink(ThirdPartyNoticesButton, narrow ? 4 : 2, 0);
    }

    private static void PositionLink(Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
    }
}
