using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Openza.Reader.Models;

namespace Openza.Reader.Pages;

public sealed partial class SettingsPage : UserControl
{
    private const double NarrowLayoutWidth = 760;
    private bool _isLoading;

    public event EventHandler? SettingsChanged;

    public event EventHandler? ClearRecentFilesClicked;

    public SettingsPage()
    {
        InitializeComponent();
    }

    public void Load(ReaderSettings settings, IReadOnlyList<RecentFileItem> recentFiles)
    {
        _isLoading = true;
        SelectByTag(DefaultViewCombo, settings.DefaultViewMode.ToString());
        SelectByTag(ReaderThemeCombo, settings.ReaderTheme.ToString());
        SelectByTag(RemoteImagesCombo, settings.RemoteImages.ToString());
        StatsSwitch.IsOn = settings.ShowDocumentStats;
        SetRecentFileCount(recentFiles.Count);
        _isLoading = false;
    }

    public void ApplyTo(ReaderSettings settings)
    {
        settings.DefaultViewMode = EnumValue(DefaultViewCombo, DocumentViewMode.Preview);
        settings.ReaderTheme = EnumValue(ReaderThemeCombo, ReaderThemeKind.System);
        settings.RemoteImages = EnumValue(RemoteImagesCombo, RemoteImagePolicy.Allow);
        settings.ShowDocumentStats = StatsSwitch.IsOn;
    }

    private void OnClearRecentFilesClicked(object sender, RoutedEventArgs e)
    {
        ClearRecentFilesClicked?.Invoke(this, EventArgs.Empty);
        SetRecentFileCount(0);
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSettingsSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width < NarrowLayoutWidth);
    }

    private void ApplyResponsiveLayout(bool isNarrow)
    {
        SettingsScrollViewer.Padding = isNarrow
            ? new Thickness(24, 0, 24, 36)
            : new Thickness(106, 0, 36, 36);
        SettingsContent.MaxWidth = isNarrow ? double.PositiveInfinity : 1064;

        SetRowLayout(DefaultViewControlColumn, DefaultViewCombo, isNarrow, stretchControl: true);
        SetRowLayout(ReaderThemeControlColumn, ReaderThemeCombo, isNarrow, stretchControl: true);
        SetRowLayout(RemoteImagesControlColumn, RemoteImagesCombo, isNarrow, stretchControl: true);
        SetRowLayout(StatsControlColumn, StatsSwitch, isNarrow, stretchControl: false);
        SetRowLayout(RecentFilesControlColumn, ClearRecentFilesButton, isNarrow, stretchControl: false, wideWidth: GridLength.Auto);
    }

    private static void SetRowLayout(
        ColumnDefinition controlColumn,
        FrameworkElement control,
        bool isNarrow,
        bool stretchControl,
        GridLength? wideWidth = null)
    {
        controlColumn.Width = isNarrow
            ? new GridLength(0)
            : wideWidth ?? new GridLength(240);

        Grid.SetRow(control, isNarrow ? 1 : 0);
        Grid.SetColumn(control, isNarrow ? 1 : 2);
        Grid.SetColumnSpan(control, isNarrow ? 2 : 1);
        control.Margin = isNarrow ? new Thickness(0, 12, 0, 0) : new Thickness(0);
        control.HorizontalAlignment = stretchControl ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
    }

    private void SetRecentFileCount(int count)
    {
        RecentFilesCountText.Text = count switch
        {
            0 => "No recent files",
            1 => "1 recent file",
            _ => $"{count} recent files"
        };
    }

    private static T EnumValue<T>(ComboBox comboBox, T fallback)
        where T : struct
    {
        return comboBox.SelectedItem is ComboBoxItem { Tag: string tag } &&
            Enum.TryParse(tag, out T result)
                ? result
                : fallback;
    }

    private static void SelectByTag(ComboBox comboBox, string tag)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = comboBox.Items.Count > 0 ? 0 : -1;
    }
}
