using Avalonia.Controls;
using Openza.Reader.Avalonia.Models;

namespace Openza.Reader.Avalonia;

public sealed partial class SettingsWindow : Window
{
    private readonly ReaderPreferences _preferences;

    public SettingsWindow()
        : this(new ReaderPreferences())
    {
    }

    public SettingsWindow(ReaderPreferences preferences)
    {
        InitializeComponent();
        _preferences = new ReaderPreferences
        {
            Theme = preferences.Theme,
            ViewMode = preferences.ViewMode,
            AllowRemoteImages = preferences.AllowRemoteImages,
            ShowDocumentStats = preferences.ShowDocumentStats,
            RecentFiles = [.. preferences.RecentFiles]
        };

        SelectTag(ThemePicker, _preferences.Theme.ToString());
        SelectTag(ViewPicker, _preferences.ViewMode.ToString());
        RemoteImagesCheckBox.IsChecked = _preferences.AllowRemoteImages;
        DocumentStatsCheckBox.IsChecked = _preferences.ShowDocumentStats;
        UpdateRecentFilesCount();
    }

    private void OnSaveClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _preferences.Theme = ParseSelection(ThemePicker, ReaderColorTheme.System);
        _preferences.ViewMode = ParseSelection(ViewPicker, ReaderViewMode.Preview);
        _preferences.AllowRemoteImages = RemoteImagesCheckBox.IsChecked == true;
        _preferences.ShowDocumentStats = DocumentStatsCheckBox.IsChecked == true;
        Close(_preferences);
    }

    private void OnCancelClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnClearRecentFilesClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _preferences.RecentFiles.Clear();
        UpdateRecentFilesCount();
    }

    private void UpdateRecentFilesCount()
    {
        RecentFilesCountText.Text = _preferences.RecentFiles.Count switch
        {
            0 => "No recent files stored on this device.",
            1 => "1 recent file stored on this device.",
            var count => $"{count} recent files stored on this device."
        };
    }

    private static T ParseSelection<T>(ComboBox comboBox, T fallback)
        where T : struct
    {
        return comboBox.SelectedItem is ComboBoxItem { Tag: string tag }
            && Enum.TryParse<T>(tag, ignoreCase: true, out var result)
            ? result
            : fallback;
    }

    private static void SelectTag(ComboBox comboBox, string value)
    {
        comboBox.SelectedItem = comboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, value, StringComparison.OrdinalIgnoreCase));
    }
}
