using Microsoft.UI.Xaml.Controls;
using Openza.Reader.Models;

namespace Openza.Reader.Controls;

public sealed partial class ReaderEmptyStateControl : UserControl
{
    public event EventHandler? OpenFileRequested;

    public event EventHandler? ClearRecentFilesRequested;

    public event EventHandler<string>? RecentFileInvoked;

    public ReaderEmptyStateControl()
    {
        InitializeComponent();
    }

    public void SetRecentFiles(IEnumerable<RecentFileItem> recentFiles)
    {
        var files = recentFiles.ToList();
        RecentFilesList.ItemsSource = files;
        var hasRecentFiles = files.Count > 0;

        RecentFilesList.Visibility = hasRecentFiles ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        NoRecentFilesPanel.Visibility = hasRecentFiles ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        ClearRecentFilesButton.IsEnabled = hasRecentFiles;
        RecentFilesHintText.Text = hasRecentFiles
            ? "Pick up where you left off."
            : "Recent files stay on this device.";
    }

    private void OnOpenFileClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OpenFileRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClearRecentFilesClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ClearRecentFilesRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecentFileClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentFileItem item)
        {
            RecentFileInvoked?.Invoke(this, item.Path);
        }
    }
}
