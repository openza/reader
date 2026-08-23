using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Openza.Reader.Avalonia.Models;

namespace Openza.Reader.Avalonia.Views;

public sealed partial class ReaderEmptyView : UserControl
{
    private const double NarrowWidth = 760;

    public event EventHandler? OpenFileRequested;

    public event EventHandler? ClearRecentFilesRequested;

    public event Action<object?, string>? RecentFileInvoked;

    public ReaderEmptyView()
    {
        InitializeComponent();
    }

    public void SetRecentFiles(IReadOnlyList<RecentFileView> recentFiles)
    {
        RecentFilesList.ItemsSource = recentFiles;
        RecentFilesList.IsVisible = recentFiles.Count > 0;
        NoRecentFiles.IsVisible = recentFiles.Count == 0;
        ClearButton.IsEnabled = recentFiles.Count > 0;
        RecentFilesHintText.Text = recentFiles.Count switch
        {
            0 => "Opened Markdown files will appear here.",
            1 => "1 recent document on this device.",
            var count => $"{count} recent documents on this device."
        };
    }

    private void OnOpenFileClicked(object? sender, RoutedEventArgs e) => OpenFileRequested?.Invoke(this, EventArgs.Empty);

    private void OnClearClicked(object? sender, RoutedEventArgs e) => ClearRecentFilesRequested?.Invoke(this, EventArgs.Empty);

    private void OnRecentFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (RecentFilesList.SelectedItem is RecentFileView item)
        {
            RecentFileInvoked?.Invoke(this, item.Path);
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < NarrowWidth;
        LayoutRoot.ColumnDefinitions[0].Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(320);
        LayoutRoot.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        LayoutRoot.RowDefinitions[0].Height = narrow ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        LayoutRoot.RowDefinitions[1].Height = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetRow(RecentCard, narrow ? 1 : 0);
        Grid.SetColumn(RecentCard, narrow ? 0 : 1);
        IdentityPanel.Margin = narrow ? new Thickness(0, 16, 0, 0) : new Thickness(0, 72, 0, 0);
        RecentCard.IsVisible = true;
    }
}
