using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Openza.Reader.Avalonia.Models;
using Openza.Reader.Avalonia.Services;
using Openza.Reader.Avalonia.Views;
using Openza.Reader.Services;
using System.Diagnostics;
using System.Globalization;
using TheArtOfDev.HtmlRenderer.Avalonia;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace Openza.Reader.Avalonia;

public sealed partial class MainWindow : Window
{
    private static readonly StringComparer FilePathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly MarkdownRenderer _markdownRenderer = new();
    private readonly ReaderPreferencesStore _preferencesStore = new();
    private ReaderPreferences _preferences;
    private string? _currentFilePath;
    private string _renderedHtmlBody = string.Empty;
    private string _lastSearchQuery = string.Empty;
    private int _nextSearchOccurrence;
    private ReaderViewMode _viewMode;
    private FileSystemWatcher? _fileWatcher;
    private CancellationTokenSource? _reloadDebounce;
    private double _zoomFactor = 1.0;
    private bool _isFocusMode;
    private WindowState _windowStateBeforeFocus = WindowState.Normal;
    private ReaderEmptyView? _activeRecentFilesView;
    private SettingsView? _activeSettingsView;
    private AboutView? _activeAboutView;
    private readonly IPlatformSettings? _platformSettings;

    public MainWindow()
        : this([])
    {
    }

    public MainWindow(string[] launchArguments)
    {
        InitializeComponent();
        ReaderHtml.LinkClicked += OnReaderLinkClicked;
        ReaderHtml.RenderError += OnReaderRenderError;
        _platformSettings = this.GetPlatformSettings();
        if (_platformSettings is not null)
        {
            _platformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
        }
        _preferences = _preferencesStore.Load();
        _viewMode = _preferences.ViewMode;
        ActualThemeVariantChanged += (_, _) => RefreshReaderStylesheet();
        EmptyState.OpenFileRequested += OnEmptyStateOpenFileRequested;
        EmptyState.ClearRecentFilesRequested += OnClearRecentFilesRequested;
        EmptyState.RecentFileInvoked += OnRecentFileInvoked;
        ApplyPreferences();
        RefreshRecentFiles();

        var launchFile = launchArguments.FirstOrDefault(IsMarkdownFile);
        if (launchFile is not null)
        {
            Opened += async (_, _) => await LoadDocumentAsync(Path.GetFullPath(launchFile), addToRecent: true);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _reloadDebounce?.Cancel();
        _reloadDebounce?.Dispose();
        _fileWatcher?.Dispose();
        if (_platformSettings is not null)
        {
            _platformSettings.ColorValuesChanged -= OnPlatformColorValuesChanged;
        }
        base.OnClosed(e);
    }

    private async void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Markdown")
                {
                    Patterns = ["*.md", "*.markdown"],
                    MimeTypes = ["text/markdown", "text/plain"]
                }
            ]
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
        {
            await LoadDocumentAsync(path, addToRecent: true);
        }
    }

    private void OnRecentClicked(object? sender, RoutedEventArgs e)
    {
        var view = new ReaderEmptyView();
        view.OpenFileRequested += OnEmptyStateOpenFileRequested;
        view.ClearRecentFilesRequested += OnClearRecentFilesRequested;
        view.RecentFileInvoked += OnRecentFileInvoked;
        view.SetRecentFiles(GetRecentFileViews());
        _activeRecentFilesView = view;
        ShowWorkspace("Recent files", "Open a recent Markdown document or choose another file.", view);
    }

    private async void OnReloadClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentFilePath is not null)
        {
            await LoadDocumentAsync(_currentFilePath, addToRecent: false);
        }
    }

    private void OnViewModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModePicker.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !Enum.TryParse<ReaderViewMode>(tag, ignoreCase: true, out var mode))
        {
            return;
        }

        _viewMode = mode;
        UpdateDocumentLayout();
    }

    private void OnTocToggled(object? sender, RoutedEventArgs e)
    {
        SetTocVisible(TocToggle.IsChecked == true);
    }

    private void OnFindClicked(object? sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void OnFindTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            FindNext();
        }
    }

    private void FindNext()
    {
        var query = FindTextBox.Text?.Trim();
        var markdown = RawMarkdown.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query) || markdown.Length == 0)
        {
            return;
        }

        if (ReaderHtml.IsVisible && _renderedHtmlBody.Length > 0)
        {
            if (!string.Equals(query, _lastSearchQuery, StringComparison.OrdinalIgnoreCase))
            {
                _lastSearchQuery = query;
                _nextSearchOccurrence = 0;
            }

            var highlighted = HtmlTextHighlighter.HighlightOccurrence(
                _renderedHtmlBody,
                query,
                _nextSearchOccurrence);
            if (!highlighted.Found)
            {
                ReaderHtml.Text = _renderedHtmlBody;
                _lastSearchQuery = query;
                _nextSearchOccurrence = 0;
                ShowMessage($"No matches found for '{query}'.");
                return;
            }

            ReaderHtml.Text = highlighted.Html;
            ReaderHtml.ScrollToElement(HtmlTextHighlighter.MatchElementId);
            _nextSearchOccurrence = highlighted.MatchIndex + 1;
            ShowMessage($"Match {highlighted.MatchIndex + 1} of {highlighted.MatchCount}.");
            return;
        }

        var startIndex = Math.Clamp(
            Math.Max(RawMarkdown.SelectionStart, RawMarkdown.SelectionEnd),
            0,
            markdown.Length);
        var matchIndex = markdown.IndexOf(query, startIndex, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0 && startIndex > 0)
        {
            matchIndex = markdown.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (matchIndex < 0)
        {
            ShowMessage($"No matches found for '{query}'.");
            return;
        }

        RawMarkdown.Focus();
        RawMarkdown.SelectionStart = matchIndex;
        RawMarkdown.SelectionEnd = matchIndex + query.Length;
        RawMarkdown.CaretIndex = matchIndex + query.Length;

        HideMessage();
    }

    private void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        var view = new SettingsView();
        view.Load(_preferences);
        view.PreferencesChanged += OnWorkspacePreferencesChanged;
        view.ClearRecentFilesRequested += OnClearRecentFilesRequested;
        _activeSettingsView = view;
        ShowWorkspace("Settings", "Reader preferences and local app data.", view);
    }

    private void OnAboutClicked(object? sender, RoutedEventArgs e)
    {
        var view = new AboutView();
        view.LinkRequested += OnAboutLinkRequested;
        _activeAboutView = view;
        ShowWorkspace(
            "About Openza Reader",
            "Version, project links, license, and security information.",
            view);
    }

    private async void OnWorkspacePreferencesChanged(object? sender, ReaderPreferences updated)
    {
        updated.RecentFiles = [.. _preferences.RecentFiles];
        _preferences = updated;
        _preferencesStore.Save(_preferences);
        ApplyPreferences();
        if (_currentFilePath is not null)
        {
            await LoadDocumentAsync(_currentFilePath, addToRecent: false);
        }
    }

    private void OnAboutLinkRequested(object? sender, string uri)
    {
        TryOpenWithDesktop(uri, "The link could not be opened by the desktop.");
    }

    private void OnZoomOutClicked(object? sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);
        RefreshReaderStylesheet();
    }

    private void OnZoomInClicked(object? sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Min(2.5, _zoomFactor + 0.1);
        RefreshReaderStylesheet();
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        var selectedText = _viewMode switch
        {
            ReaderViewMode.Raw => RawMarkdown.SelectedText,
            ReaderViewMode.Preview => ReaderHtml.SelectedText,
            ReaderViewMode.SideBySide when RawMarkdown.IsKeyboardFocusWithin => RawMarkdown.SelectedText,
            ReaderViewMode.SideBySide when ReaderHtml.IsKeyboardFocusWithin => ReaderHtml.SelectedText,
            ReaderViewMode.SideBySide => ReaderHtml.SelectedText ?? RawMarkdown.SelectedText,
            _ => null
        };

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (!string.IsNullOrEmpty(selectedText) && clipboard is not null)
        {
            await clipboard.SetTextAsync(selectedText);
        }
    }

    private void OnOpenExternalClicked(object? sender, RoutedEventArgs e)
    {
        if (_currentFilePath is null)
        {
            ShowMessage("Open a Markdown file before choosing an editor.");
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            TryOpenWithApplicationPicker(_currentFilePath);
        }
        else
        {
            TryOpenWithDesktop(_currentFilePath, "No installed application could open this Markdown file.");
        }
    }

    private void OnFocusClicked(object? sender, RoutedEventArgs e)
    {
        SetFocusMode(FocusToggle.IsChecked == true);
    }

    private void OnExitFocusClicked(object? sender, RoutedEventArgs e)
    {
        SetFocusMode(false);
    }

    private void SetFocusMode(bool enabled)
    {
        if (_isFocusMode == enabled)
        {
            return;
        }

        _isFocusMode = enabled;
        FocusToggle.IsChecked = enabled;
        if (enabled)
        {
            HideWorkspace();
        }

        HeaderBar.IsVisible = !enabled;
        CommandBar.IsVisible = !enabled;
        MessageBar.IsVisible = !enabled && !string.IsNullOrEmpty(MessageText.Text);
        FocusExitButton.IsVisible = enabled;
        SetTocVisible(!enabled && TocToggle.IsChecked == true);

        if (enabled)
        {
            _windowStateBeforeFocus = WindowState;
            WindowState = WindowState.FullScreen;
        }
        else
        {
            WindowState = _windowStateBeforeFocus;
            SetTocVisible(TocList.ItemCount > 0 && Bounds.Width >= 900);
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            e.Handled = true;
            SetFocusMode(!_isFocusMode);
        }
        else if (e.Key == Key.Escape && _isFocusMode)
        {
            e.Handled = true;
            SetFocusMode(false);
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.F)
        {
            e.Handled = true;
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.O)
        {
            e.Handled = true;
            OnOpenClicked(this, new RoutedEventArgs());
        }
    }

    private void OnTocSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TocList.SelectedItem is TocItemView item && ReaderHtml.IsVisible)
        {
            ReaderHtml.ScrollToElement(item.Id);
        }
    }

    private void OnReaderLinkClicked(
        object? sender,
        HtmlRendererRoutedEventArgs<HtmlLinkClickedEventArgs> args)
    {
        args.Event.Handled = true;
        var link = args.Event.Link?.Trim();
        if (string.IsNullOrEmpty(link))
        {
            return;
        }

        if (link.StartsWith('#'))
        {
            var fragment = link[1..];
            if (string.Equals(fragment, "blocked-link", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("Openza Reader blocked this link.");
                return;
            }

            try
            {
                ReaderHtml.ScrollToElement(Uri.UnescapeDataString(fragment));
            }
            catch (UriFormatException)
            {
                ShowMessage("Openza Reader could not navigate to that document heading.");
            }

            return;
        }

        if (Uri.TryCreate(link, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeMailto))
        {
            TryOpenWithDesktop(uri.AbsoluteUri, "The link could not be opened by the desktop.");
            return;
        }

        ShowMessage("Openza Reader blocked a link with an unsupported scheme.");
    }

    private void OnReaderRenderError(
        object? sender,
        HtmlRendererRoutedEventArgs<HtmlRenderErrorEventArgs> args)
    {
        ShowMessage("Some Markdown content could not be rendered safely.");
    }

    private async void OnFilesDropped(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        var path = files?.Select(file => file.TryGetLocalPath()).FirstOrDefault(IsMarkdownFile);
        if (path is not null)
        {
            await LoadDocumentAsync(path, addToRecent: true);
        }
    }

    private async Task LoadDocumentAsync(string path, bool addToRecent)
    {
        try
        {
            if (!File.Exists(path) || !IsMarkdownFile(path))
            {
                ShowMessage("Choose an existing .md or .markdown file.");
                return;
            }

            var preservedScrollOffset = !addToRecent && FilePathComparer.Equals(_currentFilePath, path)
                ? GetReaderScrollViewer()?.Offset
                : null;
            var markdown = await File.ReadAllTextAsync(path);
            var result = _markdownRenderer.Render(markdown, path, _preferences.AllowRemoteImages);

            _currentFilePath = path;
            if (addToRecent)
            {
                HideWorkspace();
                _viewMode = _preferences.ViewMode;
                SelectTag(ViewModePicker, _viewMode.ToString());
            }
            RawMarkdown.Text = markdown;
            HideMessage();
            _renderedHtmlBody = result.HtmlBody;
            _lastSearchQuery = string.Empty;
            _nextSearchOccurrence = 0;
            ReaderHtml.Text = _renderedHtmlBody;
            var windowTitle = $"{Path.GetFileName(path)} - Openza Reader";
            DocumentTitle.Text = windowTitle;
            Title = windowTitle;
            DocumentStatsText.Text = _preferences.ShowDocumentStats
                ? FormatDocumentStats(result.Stats.WordCount, result.Stats.EstimatedReadMinutes, result.Stats.HeadingCount)
                : string.Empty;
            TocList.ItemsSource = result.TocItems.Select(item => new TocItemView(item.Id, item.Title, item.Level)).ToList();
            TocToggle.IsEnabled = result.TocItems.Count > 0;
            DocumentHost.IsVisible = true;
            EmptyState.IsVisible = false;
            UpdateDocumentLayout();
            if (preservedScrollOffset is { } offset)
            {
                Dispatcher.UIThread.Post(
                    () => RestoreReaderScrollOffset(offset),
                    DispatcherPriority.Loaded);
            }
            SetTocVisible(result.TocItems.Count > 0 && Bounds.Width >= 900);
            ConfigureFileWatcher(path);

            if (addToRecent)
            {
                _preferencesStore.AddRecentFile(_preferences, path);
                RefreshRecentFiles();
            }
        }
        catch (UnauthorizedAccessException)
        {
            ShowMessage("Openza Reader does not have permission to read that file.");
        }
        catch (IOException exception)
        {
            ShowMessage($"The document could not be opened: {exception.Message}");
        }
    }

    private void ConfigureFileWatcher(string path)
    {
        _fileWatcher?.Dispose();
        _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };
        _fileWatcher.Changed += OnWatchedFileChanged;
        _fileWatcher.Created += OnWatchedFileChanged;
        _fileWatcher.Renamed += OnWatchedFileChanged;
        _fileWatcher.Deleted += OnWatchedFileChanged;
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        _reloadDebounce?.Cancel();
        _reloadDebounce?.Dispose();
        var debounce = _reloadDebounce = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, debounce.Token);
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_currentFilePath is not null)
                    {
                        await LoadDocumentAsync(_currentFilePath, addToRecent: false);
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private void UpdateDocumentLayout()
    {
        if (!DocumentHost.IsVisible)
        {
            return;
        }

        var showRaw = _viewMode is ReaderViewMode.Raw or ReaderViewMode.SideBySide;
        var showPreview = _viewMode is ReaderViewMode.Preview or ReaderViewMode.SideBySide;
        RawMarkdown.IsVisible = showRaw;
        ReaderHtml.IsVisible = showPreview;
        DocumentSplitter.IsVisible = _viewMode == ReaderViewMode.SideBySide;
        DocumentHost.ColumnDefinitions[0].Width = showRaw ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        DocumentHost.ColumnDefinitions[1].Width = DocumentSplitter.IsVisible ? new GridLength(5) : new GridLength(0);
        DocumentHost.ColumnDefinitions[2].Width = showPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        TocToggle.IsEnabled = showPreview && TocList.ItemCount > 0;
        if (!showPreview)
        {
            SetTocVisible(false);
        }
    }

    private void ApplyPreferences()
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = IsHighContrastEnabled()
                ? ThemeVariant.Default
                : _preferences.Theme switch
            {
                ReaderColorTheme.Light => ThemeVariant.Light,
                ReaderColorTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        SelectTag(ViewModePicker, _viewMode.ToString());
        UpdateDocumentLayout();
        RefreshReaderStylesheet();
    }

    private void RefreshReaderStylesheet()
    {
        var palette = ResolveReaderPalette();
        var fontSize = 16 * _zoomFactor;
        var formattedFontSize = fontSize.ToString("0.#", CultureInfo.InvariantCulture);

        ReaderHtml.Background = new SolidColorBrush(Color.Parse(palette.Background));
        ReaderHtml.BaseStylesheet = string.Join(
            Environment.NewLine,
            [
                $"html, body {{ margin: 0; padding: 0; color: {palette.Foreground}; background-color: {palette.Background}; font-family: \"Segoe UI\", Inter, Arial, sans-serif; font-size: {formattedFontSize}px; line-height: 1.6; }}",
                $"p, li, td, th, blockquote {{ color: {palette.Foreground}; }}",
                $"h1, h2, h3, h4, h5, h6 {{ color: {palette.Foreground}; margin-top: 1.25em; margin-bottom: 0.55em; }}",
                $"h1, h2 {{ border-bottom: 1px solid {palette.Border}; padding-bottom: 0.3em; }}",
                $"a {{ color: {palette.Link}; text-decoration: underline; }}",
                $"blockquote {{ margin-left: 0; padding-left: 1em; border-left: 4px solid {palette.Border}; color: {palette.Muted}; }}",
                $"pre {{ display: block; padding: 1em; background-color: {palette.CodeBackground}; border: 1px solid {palette.Border}; white-space: pre-wrap; }}",
                $"code {{ font-family: \"Cascadia Mono\", \"DejaVu Sans Mono\", monospace; background-color: {palette.CodeBackground}; }}",
                $"table {{ border-collapse: collapse; margin: 1em 0; }}",
                $"th, td {{ border: 1px solid {palette.Border}; padding: 0.45em 0.7em; }}",
                $".openza-search-match {{ background-color: {palette.SelectionBackground}; color: {palette.SelectionForeground}; }}",
                "img { max-width: 100%; }"
            ]);
    }

    private ReaderPalette ResolveReaderPalette()
    {
        if (IsHighContrastEnabled())
        {
            return ResolveHighContrastPalette();
        }

        var theme = _preferences.Theme == ReaderColorTheme.System
            ? ActualThemeVariant == ThemeVariant.Dark ? ReaderColorTheme.Dark : ReaderColorTheme.Light
            : _preferences.Theme;

        return theme switch
        {
            ReaderColorTheme.Dark => new ReaderPalette(
                "#0D1117",
                "#E6EDF3",
                "#8B949E",
                "#30363D",
                "#161B22",
                "#58A6FF",
                "#F2CC60",
                "#0D1117"),
            ReaderColorTheme.Sepia => new ReaderPalette(
                "#F4ECD8",
                "#3E3427",
                "#74644F",
                "#C9B995",
                "#E8DDC3",
                "#8B5A2B",
                "#C88B24",
                "#1F1A12"),
            _ => new ReaderPalette(
                "#FFFFFF",
                "#1F2328",
                "#59636E",
                "#D0D7DE",
                "#F6F8FA",
                "#0969DA",
                "#FFF3A3",
                "#1F2328")
        };
    }

    private bool IsHighContrastEnabled() =>
        _platformSettings?.GetColorValues().ContrastPreference == ColorContrastPreference.High;

    private static ReaderPalette ResolveHighContrastPalette()
    {
#if WINDOWS
        return new ReaderPalette(
            ToCssColor(System.Drawing.SystemColors.Window),
            ToCssColor(System.Drawing.SystemColors.WindowText),
            ToCssColor(System.Drawing.SystemColors.GrayText),
            ToCssColor(System.Drawing.SystemColors.WindowText),
            ToCssColor(System.Drawing.SystemColors.Window),
            ToCssColor(System.Drawing.SystemColors.HotTrack),
            ToCssColor(System.Drawing.SystemColors.Highlight),
            ToCssColor(System.Drawing.SystemColors.HighlightText));
#else
        return new ReaderPalette(
            "#000000", "#FFFFFF", "#FFFFFF", "#FFFFFF",
            "#000000", "#FFFF00", "#FFFFFF", "#000000");
#endif
    }

#if WINDOWS
    private static string ToCssColor(System.Drawing.Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";
#endif

    private void OnPlatformColorValuesChanged(object? sender, PlatformColorValues e)
    {
        Dispatcher.UIThread.Post(ApplyPreferences);
    }

    private ScrollViewer? GetReaderScrollViewer() =>
        ReaderHtml.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

    private void RestoreReaderScrollOffset(Vector offset)
    {
        if (GetReaderScrollViewer() is { } scrollViewer)
        {
            scrollViewer.Offset = offset;
        }
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width < 900)
        {
            SetTocVisible(false);
        }
        else if (TocToggle.IsChecked == true)
        {
            SetTocVisible(true);
        }
    }

    private void SetTocVisible(bool visible)
    {
        var canShow = !_isFocusMode
            && !PageWorkspace.IsVisible
            && Bounds.Width >= 900
            && _viewMode is ReaderViewMode.Preview or ReaderViewMode.SideBySide
            && TocList.ItemCount > 0;
        TocPane.IsVisible = visible && canShow;
        TocToggle.IsChecked = TocPane.IsVisible;
    }

    private void ShowWorkspace(string title, string subtitle, Control content)
    {
        SetFocusMode(false);
        PageWorkspaceTitle.Text = title;
        PageWorkspaceSubtitle.Text = subtitle;
        PageWorkspaceContent.Content = content;
        PageWorkspace.IsVisible = true;
        SetTocVisible(false);
    }

    private void HideWorkspace()
    {
        if (_activeRecentFilesView is not null)
        {
            _activeRecentFilesView.OpenFileRequested -= OnEmptyStateOpenFileRequested;
            _activeRecentFilesView.ClearRecentFilesRequested -= OnClearRecentFilesRequested;
            _activeRecentFilesView.RecentFileInvoked -= OnRecentFileInvoked;
            _activeRecentFilesView = null;
        }

        if (_activeSettingsView is not null)
        {
            _activeSettingsView.PreferencesChanged -= OnWorkspacePreferencesChanged;
            _activeSettingsView.ClearRecentFilesRequested -= OnClearRecentFilesRequested;
            _activeSettingsView = null;
        }

        if (_activeAboutView is not null)
        {
            _activeAboutView.LinkRequested -= OnAboutLinkRequested;
            _activeAboutView = null;
        }

        PageWorkspaceContent.Content = null;
        PageWorkspace.IsVisible = false;
        SetTocVisible(TocList.ItemCount > 0 && Bounds.Width >= 900);
    }

    private void OnCloseWorkspaceClicked(object? sender, RoutedEventArgs e) => HideWorkspace();

    private void OnDismissMessageClicked(object? sender, RoutedEventArgs e) => HideMessage();

    private void OnEmptyStateOpenFileRequested(object? sender, EventArgs e) => OnOpenClicked(this, new RoutedEventArgs());

    private async void OnRecentFileInvoked(object? sender, string path) => await LoadDocumentAsync(path, addToRecent: true);

    private void OnClearRecentFilesRequested(object? sender, EventArgs e)
    {
        _preferences.RecentFiles.Clear();
        _preferencesStore.Save(_preferences);
        RefreshRecentFiles();
    }

    private void RefreshRecentFiles()
    {
        _preferences.RecentFiles = _preferences.RecentFiles
            .Where(File.Exists)
            .Distinct(FilePathComparer)
            .Take(8)
            .ToList();
        var recentFiles = GetRecentFileViews();
        EmptyState.SetRecentFiles(recentFiles);
        _activeRecentFilesView?.SetRecentFiles(recentFiles);
        _activeSettingsView?.SetRecentFileCount(recentFiles.Count);
    }

    private IReadOnlyList<RecentFileView> GetRecentFileViews() =>
        _preferences.RecentFiles.Select(path => new RecentFileView(path)).ToList();

    private static string FormatDocumentStats(int wordCount, int estimatedReadMinutes, int headingCount)
    {
        var readTime = estimatedReadMinutes == 1 ? "1 min read" : $"{estimatedReadMinutes} min read";
        var headings = headingCount == 1 ? "1 heading" : $"{headingCount:N0} headings";
        return $"{wordCount:N0} words - {readTime} - {headings}";
    }

    private void TryOpenWithDesktop(string target, string errorMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ShowMessage(errorMessage);
        }
    }

    private void TryOpenWithApplicationPicker(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "openas"
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ShowMessage("Windows could not find an app to edit this Markdown file.");
        }
    }

    private static bool IsMarkdownFile(string? path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase);
    }

    private static void SelectTag(ComboBox comboBox, string value)
    {
        comboBox.SelectedItem = comboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, value, StringComparison.OrdinalIgnoreCase));
    }

    private void ShowMessage(string message)
    {
        MessageText.Text = message;
        MessageBar.IsVisible = true;
    }

    private void HideMessage()
    {
        MessageBar.IsVisible = false;
    }

    private sealed record ReaderPalette(
        string Background,
        string Foreground,
        string Muted,
        string Border,
        string CodeBackground,
        string Link,
        string SelectionBackground,
        string SelectionForeground);
}
