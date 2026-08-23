using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Openza.Reader.Avalonia.Models;

namespace Openza.Reader.Avalonia.Views;

public sealed partial class SettingsView : UserControl
{
    private const double NarrowWidth = 760;
    private bool _isLoading;

    public event Action<object?, ReaderPreferences>? PreferencesChanged;

    public event EventHandler? ClearRecentFilesRequested;

    public SettingsView()
    {
        InitializeComponent();
    }

    public void Load(ReaderPreferences preferences)
    {
        _isLoading = true;
        SelectTag(DefaultViewPicker, preferences.ViewMode.ToString());
        SelectTag(ThemePicker, preferences.Theme.ToString());
        SelectTag(RemoteImagesPicker, preferences.AllowRemoteImages ? "Allow" : "Block");
        DocumentStatsCheckBox.IsChecked = preferences.ShowDocumentStats;
        SetRecentFileCount(preferences.RecentFiles.Count);
        _isLoading = false;
    }

    public void SetRecentFileCount(int count)
    {
        RecentFilesCountText.Text = count switch
        {
            0 => "No recent files",
            1 => "1 recent file",
            _ => $"{count} recent files"
        };
        ClearRecentFilesButton.IsEnabled = count > 0;
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        PreferencesChanged?.Invoke(this, new ReaderPreferences
        {
            ViewMode = ParseSelection(DefaultViewPicker, ReaderViewMode.Preview),
            Theme = ParseSelection(ThemePicker, ReaderColorTheme.System),
            AllowRemoteImages = string.Equals(SelectedTag(RemoteImagesPicker), "Allow", StringComparison.OrdinalIgnoreCase),
            ShowDocumentStats = DocumentStatsCheckBox.IsChecked == true
        });
    }

    private void OnClearRecentFilesClicked(object? sender, RoutedEventArgs e) => ClearRecentFilesRequested?.Invoke(this, EventArgs.Empty);

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < NarrowWidth;
        SettingsScrollViewer.Padding = narrow ? new Thickness(24, 0, 24, 36) : new Thickness(106, 0, 36, 36);
        SettingsContent.MaxWidth = narrow ? double.PositiveInfinity : 1064;
        ApplyRowLayout(DefaultViewRow, DefaultViewPicker, narrow);
        ApplyRowLayout(ThemeRow, ThemePicker, narrow);
        ApplyRowLayout(RemoteImagesRow, RemoteImagesPicker, narrow);
        ApplyRowLayout(StatsRow, DocumentStatsCheckBox, narrow);
        ApplyRowLayout(RecentFilesRow, ClearRecentFilesButton, narrow, autoWide: true);
    }

    private static void ApplyRowLayout(Grid row, Control control, bool narrow, bool autoWide = false)
    {
        row.ColumnDefinitions[2].Width = narrow ? new GridLength(0) : autoWide ? GridLength.Auto : new GridLength(240);
        Grid.SetRow(control, narrow ? 1 : 0);
        Grid.SetColumn(control, narrow ? 1 : 2);
        Grid.SetColumnSpan(control, narrow ? 2 : 1);
        control.Margin = narrow ? new Thickness(0, 12, 0, 0) : new Thickness(0);
        control.HorizontalAlignment = narrow && !autoWide ? global::Avalonia.Layout.HorizontalAlignment.Stretch : global::Avalonia.Layout.HorizontalAlignment.Left;
    }

    private static T ParseSelection<T>(ComboBox comboBox, T fallback)
        where T : struct
    {
        return Enum.TryParse<T>(SelectedTag(comboBox), ignoreCase: true, out var result) ? result : fallback;
    }

    private static string? SelectedTag(ComboBox comboBox) => (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;

    private static void SelectTag(ComboBox comboBox, string value)
    {
        comboBox.SelectedItem = comboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, value, StringComparison.OrdinalIgnoreCase));
    }
}
