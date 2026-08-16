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
using Openza.Reader.Avalonia.Models;
using Openza.Reader.Avalonia.Services;
using Openza.Reader.Services;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Openza.Reader.Avalonia;

public sealed partial class MainWindow : Window
{
    private readonly MarkdownRenderer _markdownRenderer = new();
    private readonly HtmlShellBuilder _htmlShellBuilder = new();
    private readonly ReaderPreferencesStore _preferencesStore = new();
    private readonly TempHtmlDocumentStore _htmlDocumentStore;
    private ReaderPreferences _preferences;
    private string? _currentFilePath;
    private Uri? _currentHtmlUri;
    private FileSystemWatcher? _fileWatcher;
    private CancellationTokenSource? _reloadDebounce;
    private double _zoomFactor = 1.0;
    private bool _isFocusMode;
    private WindowState _windowStateBeforeFocus = WindowState.Normal;

    public MainWindow()
        : this([])
    {
    }

    public MainWindow(string[] launchArguments)
    {
        InitializeComponent();
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Openza",
            "Reader");
        _htmlDocumentStore = new TempHtmlDocumentStore(cacheRoot);
        _preferences = _preferencesStore.Load();
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

        _preferences.ViewMode = mode;
        UpdateDocumentLayout();
        _preferencesStore.Save(_preferences);
    }

    private void OnTocToggled(object? sender, RoutedEventArgs e)
    {
        TocPane.IsVisible = TocToggle.IsChecked == true && TocList.ItemCount > 0;
    }

    private async void OnFindClicked(object? sender, RoutedEventArgs e)
    {
        await FindNextAsync();
    }

    private async void OnFindTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await FindNextAsync();
        }
    }

    private async Task FindNextAsync()
    {
        var query = FindTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(query) || !ReaderWebView.IsVisible)
        {
            return;
        }

        var encoded = JsonSerializer.Serialize(query);
        await ReaderWebView.InvokeScript($"window.find({encoded}, false, false, true, false, true, false)");
    }

    private async void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_preferences);
        var result = await window.ShowDialog<ReaderPreferences?>(this);
        if (result is null)
        {
            return;
        }

        _preferences = result;
        _preferencesStore.Save(_preferences);
        ApplyPreferences();
        RefreshRecentFiles();
        if (_currentFilePath is not null)
        {
            await LoadDocumentAsync(_currentFilePath, addToRecent: false);
        }
    }

    private async void OnAboutClicked(object? sender, RoutedEventArgs e)
    {
        await new AboutWindow().ShowDialog(this);
    }

    private async void OnZoomOutClicked(object? sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);
        await ApplyZoomAsync();
    }

    private async void OnZoomInClicked(object? sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Min(2.5, _zoomFactor + 0.1);
        await ApplyZoomAsync();
    }

    private async Task ApplyZoomAsync()
    {
        if (ReaderWebView.IsVisible)
        {
            var zoom = _zoomFactor.ToString(CultureInfo.InvariantCulture);
            await ReaderWebView.InvokeScript($"document.documentElement.style.zoom = '{zoom}'");
        }
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        var selectedText = RawMarkdown.SelectedText;
        if (string.IsNullOrEmpty(selectedText) && ReaderWebView.IsVisible)
        {
            var scriptResult = await ReaderWebView.InvokeScript("window.getSelection().toString()");
            selectedText = NormalizeScriptString(scriptResult);
        }

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

        TryOpenWithDesktop(_currentFilePath, "No installed application could open this Markdown file.");
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
        HeaderBar.IsVisible = !enabled;
        CommandBar.IsVisible = !enabled;
        MessageBar.IsVisible = !enabled && !string.IsNullOrEmpty(MessageText.Text);
        StatusBar.IsVisible = !enabled;
        FocusExitButton.IsVisible = enabled;
        TocPane.IsVisible = !enabled && TocToggle.IsChecked == true && TocList.ItemCount > 0;

        if (enabled)
        {
            _windowStateBeforeFocus = WindowState;
            WindowState = WindowState.FullScreen;
        }
        else
        {
            WindowState = _windowStateBeforeFocus;
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

    private async void OnRecentFileActivated(object? sender, TappedEventArgs e)
    {
        if (RecentFilesList.SelectedItem is RecentFileView item)
        {
            await LoadDocumentAsync(item.Path, addToRecent: true);
        }
    }

    private async void OnTocSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TocList.SelectedItem is not TocItemView item || !ReaderWebView.IsVisible)
        {
            return;
        }

        var id = JsonSerializer.Serialize(item.Id);
        await ReaderWebView.InvokeScript($"document.getElementById({id})?.scrollIntoView({{behavior:'smooth',block:'start'}})");
    }

    private void OnWebViewEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        if (OperatingSystem.IsLinux() && e is LinuxWpeWebViewEnvironmentRequestedEventArgs linuxEnvironment)
        {
            linuxEnvironment.PreferWebKitGtkInstead = true;
        }

        if (OperatingSystem.IsLinux() && e is GtkWebViewEnvironmentRequestedEventArgs gtkEnvironment)
        {
            gtkEnvironment.ExperimentalOffscreen = true;
            gtkEnvironment.EphemeralDataManager = true;
            gtkEnvironment.DisableCache = true;
        }
    }

    private void OnWebViewNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        var uri = e.Request;
        if (uri is not null && !IsCurrentGeneratedDocument(uri))
        {
            e.Cancel = true;
            if (uri.Scheme is "http" or "https" or "mailto")
            {
                TryOpenWithDesktop(uri.AbsoluteUri, "The link could not be opened by the desktop.");
            }

            return;
        }

        ShowMessage("Rendering document…");
    }

    private void OnWebViewNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            ShowMessage("The Markdown preview could not be rendered by the system WebView.");
            return;
        }

        HideMessage();
        _ = ApplyZoomAsync();
    }

    private void OnWebViewNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        var uri = e.Request;
        if (uri?.Scheme is "http" or "https" or "mailto")
        {
            TryOpenWithDesktop(uri.AbsoluteUri, "The link could not be opened by the desktop.");
        }
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

            var markdown = await File.ReadAllTextAsync(path);
            var result = _markdownRenderer.Render(markdown, path, _preferences.AllowRemoteImages);
            var readerTheme = ResolveReaderTheme();
            var html = _htmlShellBuilder.Build(result, Path.GetFileName(path), readerTheme, _preferences.AllowRemoteImages);
            var htmlUri = await _htmlDocumentStore.WriteAsync(path, html);

            _currentFilePath = path;
            _currentHtmlUri = htmlUri;
            RawMarkdown.Text = markdown;
            ReaderWebView.Source = htmlUri;
            DocumentTitle.Text = result.Title ?? Path.GetFileNameWithoutExtension(path);
            Title = $"{DocumentTitle.Text} — Openza Reader";
            FilePathText.Text = path;
            DocumentStatsText.Text = _preferences.ShowDocumentStats
                ? $"{result.Stats.WordCount:N0} words  •  {result.Stats.EstimatedReadMinutes} min read  •  {result.Stats.HeadingCount} headings"
                : string.Empty;
            TocList.ItemsSource = result.TocItems.Select(item => new TocItemView(item.Id, item.Title, item.Level)).ToList();
            TocToggle.IsEnabled = result.TocItems.Count > 0;
            DocumentHost.IsVisible = true;
            EmptyState.IsVisible = false;
            UpdateDocumentLayout();
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

        var showRaw = _preferences.ViewMode is ReaderViewMode.Raw or ReaderViewMode.SideBySide;
        var showPreview = _preferences.ViewMode is ReaderViewMode.Preview or ReaderViewMode.SideBySide;
        RawMarkdown.IsVisible = showRaw;
        ReaderWebView.IsVisible = showPreview;
        DocumentSplitter.IsVisible = _preferences.ViewMode == ReaderViewMode.SideBySide;
        DocumentHost.ColumnDefinitions[0].Width = showRaw ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        DocumentHost.ColumnDefinitions[1].Width = DocumentSplitter.IsVisible ? new GridLength(5) : new GridLength(0);
        DocumentHost.ColumnDefinitions[2].Width = showPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
    }

    private void ApplyPreferences()
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = _preferences.Theme switch
            {
                ReaderColorTheme.Light => ThemeVariant.Light,
                ReaderColorTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        SelectTag(ViewModePicker, _preferences.ViewMode.ToString());
        UpdateDocumentLayout();
    }

    private void RefreshRecentFiles()
    {
        _preferences.RecentFiles = _preferences.RecentFiles.Where(File.Exists).Distinct().Take(8).ToList();
        RecentFilesList.ItemsSource = _preferences.RecentFiles.Select(path => new RecentFileView(path)).ToList();
        RecentFilesList.IsVisible = _preferences.RecentFiles.Count > 0;
    }

    private string ResolveReaderTheme()
    {
        return _preferences.Theme switch
        {
            ReaderColorTheme.Light => "light",
            ReaderColorTheme.Dark => "dark",
            ReaderColorTheme.Sepia => "sepia",
            _ => ActualThemeVariant == ThemeVariant.Dark ? "dark" : "light"
        };
    }

    private bool IsCurrentGeneratedDocument(Uri uri)
    {
        return _currentHtmlUri is not null
            && uri.IsFile
            && string.Equals(uri.LocalPath, _currentHtmlUri.LocalPath, StringComparison.Ordinal);
    }

    private static string? NormalizeScriptString(string? value)
    {
        if (string.IsNullOrEmpty(value) || value[0] != '"')
        {
            return value;
        }

        try
        {
            return JsonSerializer.Deserialize<string>(value);
        }
        catch (JsonException)
        {
            return value;
        }
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
}
