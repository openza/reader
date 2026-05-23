using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Openza.Reader;
using Openza.Reader.Controls;
using Openza.Reader.Models;
using Openza.Reader.Pages;
using Openza.Reader.Services;
using Openza.Reader.ViewModels;
using System.Globalization;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace Openza.Reader.Shell;

public sealed partial class ReaderShell : UserControl
{
    private readonly Window _owner;
    private readonly MarkdownRenderer _renderer;
    private readonly HtmlShellBuilder _shellBuilder;
    private readonly TempHtmlDocumentStore _documentStore;
    private readonly ExternalEditorService _externalEditor;
    private readonly AppSettingsService _settingsService;
    private readonly MainWindowViewModel _viewModel = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _reloadTimer;
    private ReaderSettings _readerSettings;
    private FileSystemWatcher? _watcher;
    private string? _currentFilePath;
    private Uri? _currentHtmlUri;
    private DocumentViewMode _viewMode;
    private SettingsPage? _activeSettingsPage;
    private ReaderEmptyStateControl? _activeRecentFilesPage;
    private bool _isFocusMode;
    private double _zoomFactor = 1.0;

    public ReaderShell(
        Window owner,
        MarkdownRenderer renderer,
        HtmlShellBuilder shellBuilder,
        TempHtmlDocumentStore documentStore,
        ExternalEditorService externalEditor,
        AppSettingsService settingsService)
    {
        _owner = owner;
        _renderer = renderer;
        _shellBuilder = shellBuilder;
        _documentStore = documentStore;
        _externalEditor = externalEditor;
        _settingsService = settingsService;
        _readerSettings = _settingsService.Load();
        _viewMode = _readerSettings.DefaultViewMode;

        InitializeComponent();
        TocPane.TocItemInvoked += OnTocItemInvoked;
        EmptyState.OpenFileRequested += OnEmptyStateOpenFileRequested;
        EmptyState.ClearRecentFilesRequested += OnEmptyStateClearRecentFilesRequested;
        EmptyState.RecentFileInvoked += OnRecentFileInvoked;
        AddKeyboardAccelerators();
        RefreshRecentFiles();
        SetViewMode(_viewMode);

        _reloadTimer = DispatcherQueue.CreateTimer();
        _reloadTimer.Interval = TimeSpan.FromMilliseconds(350);
        _reloadTimer.Tick += async (_, _) =>
        {
            _reloadTimer.Stop();
            await ReloadAsync(preserveScroll: true);
        };
    }

    public FrameworkElement TitleBarElement => AppTitleBar;

    public async Task<bool> EnsureWebViewAsync()
    {
        if (ReaderWebView.CoreWebView2 is not null)
        {
            return true;
        }

        try
        {
            await ReaderWebView.EnsureCoreWebView2Async();
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
            ShowWebViewRuntimeUnavailable();
            return false;
        }

        var core = ReaderWebView.CoreWebView2 ?? throw new InvalidOperationException("WebView2 failed to initialize.");
        var settings = core.Settings;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;
        settings.IsScriptEnabled = true;
        settings.IsStatusBarEnabled = false;
        settings.AreDevToolsEnabled = false;
        core.NavigationStarting += OnNavigationStarting;
        return true;
    }

    public async Task OpenFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ShowInfo("File not found", "The selected Markdown file no longer exists.", InfoBarSeverity.Warning);
            return;
        }

        _currentFilePath = filePath;
        HideWorkspace();
        SetViewMode(_readerSettings.DefaultViewMode);
        SetWindowTitle($"{Path.GetFileName(filePath)} - Openza Reader");
        await RenderCurrentFileAsync(preserveScroll: false);
        StartWatcher(filePath);
        _settingsService.AddRecentFile(filePath);
        RefreshRecentFiles();
    }

    public void Close()
    {
        _watcher?.Dispose();
    }

    private void AddKeyboardAccelerators()
    {
        var find = new KeyboardAccelerator { Key = VirtualKey.F, Modifiers = VirtualKeyModifiers.Control };
        find.Invoked += async (_, args) =>
        {
            args.Handled = true;
            await StartFindAsync();
        };
        KeyboardAccelerators.Add(find);

        var focus = new KeyboardAccelerator { Key = VirtualKey.F11 };
        focus.Invoked += (_, args) =>
        {
            args.Handled = true;
            SetFocusMode(!_isFocusMode);
        };
        KeyboardAccelerators.Add(focus);

        var exitFocus = new KeyboardAccelerator { Key = VirtualKey.Escape };
        exitFocus.Invoked += (_, args) =>
        {
            if (FocusButton.IsChecked == true)
            {
                args.Handled = true;
                SetFocusMode(false);
            }
        };
        KeyboardAccelerators.Add(exitFocus);
    }

    private void SetWindowTitle(string title)
    {
        _owner.Title = title;
        TitleTextBlock.Text = title;
    }

    private async Task RenderCurrentFileAsync(bool preserveScroll)
    {
        if (_currentFilePath is null)
        {
            return;
        }

        if (!await EnsureWebViewAsync())
        {
            return;
        }

        var scrollState = preserveScroll ? await CaptureScrollStateAsync() : null;
        var markdown = await File.ReadAllTextAsync(_currentFilePath);
        var allowRemoteImages = _readerSettings.RemoteImages == RemoteImagePolicy.Allow;
        var result = _renderer.Render(markdown, _currentFilePath, allowRemoteImages);
        var html = _shellBuilder.Build(
            result,
            Path.GetFileName(_currentFilePath),
            ReaderThemeName(_readerSettings.ReaderTheme),
            allowRemoteImages);
        _currentHtmlUri = await _documentStore.WriteAsync(_currentFilePath, html);

        RawMarkdownTextBox.Text = markdown;
        _viewModel.SetDocument(result);
        TocPane.SetItems(_viewModel.TocItems);
        EmptyState.Visibility = Visibility.Collapsed;
        UpdateStats(result.Stats);
        SetTocVisible(_viewModel.TocItems.Count > 0 && ActualWidth >= 900 && CanShowToc());
        ApplyViewMode();
        ReaderWebView.CoreWebView2.Navigate(_currentHtmlUri.AbsoluteUri);

        if (scrollState is not null)
        {
            ReaderWebView.CoreWebView2.NavigationCompleted += RestoreAfterNavigation;
        }
        else
        {
            ReaderWebView.CoreWebView2.NavigationCompleted += ApplyZoomAfterNavigation;
        }

        async void RestoreAfterNavigation(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            sender.NavigationCompleted -= RestoreAfterNavigation;
            await ApplyZoomAsync();
            await RestoreScrollStateAsync(scrollState);
        }

        async void ApplyZoomAfterNavigation(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            sender.NavigationCompleted -= ApplyZoomAfterNavigation;
            await ApplyZoomAsync();
        }
    }

    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri))
        {
            args.Cancel = true;
            return;
        }

        if (IsCurrentGeneratedDocument(uri))
        {
            return;
        }

        if (uri.Scheme is "http" or "https" or "mailto")
        {
            args.Cancel = true;
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
            return;
        }

        args.Cancel = true;
    }

    private bool IsCurrentGeneratedDocument(Uri uri)
    {
        return _currentHtmlUri is not null
            && uri.IsFile
            && string.Equals(uri.LocalPath, _currentHtmlUri.LocalPath, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> CaptureScrollStateAsync()
    {
        if (ReaderWebView.CoreWebView2 is null)
        {
            return null;
        }

        return await ReaderWebView.CoreWebView2.ExecuteScriptAsync(
            "({ y: window.scrollY, h: Math.max(1, document.documentElement.scrollHeight - window.innerHeight) })");
    }

    private async Task RestoreScrollStateAsync(string? scrollJson)
    {
        if (string.IsNullOrWhiteSpace(scrollJson) || ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        var script = """
            (() => {
              const rawState = __SCROLL_STATE__;
              const state = typeof rawState === 'string' ? JSON.parse(rawState) : rawState;
              const max = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
              const ratio = state.h > 0 ? state.y / state.h : 0;
              window.scrollTo(0, Math.max(0, max * ratio));
            })();
            """.Replace("__SCROLL_STATE__", scrollJson, StringComparison.Ordinal);
        await ReaderWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void StartWatcher(string filePath)
    {
        _watcher?.Dispose();
        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);
        if (directory is null)
        {
            return;
        }

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += QueueReload;
        _watcher.Created += QueueReload;
        _watcher.Renamed += QueueReload;
        _watcher.Deleted += QueueReload;
        _watcher.EnableRaisingEvents = true;
    }

    private void QueueReload(object sender, FileSystemEventArgs e)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                EmptyState.Visibility = Visibility.Visible;
                RawMarkdownTextBox.Text = string.Empty;
                SetTocVisible(false);
                ShowInfo("File deleted", "The current file was deleted outside Openza Reader.", InfoBarSeverity.Warning);
                return;
            }

            _reloadTimer.Stop();
            _reloadTimer.Start();
        });
    }

    private async Task ReloadAsync(bool preserveScroll)
    {
        if (_currentFilePath is null)
        {
            return;
        }

        if (File.Exists(_currentFilePath))
        {
            await RenderCurrentFileAsync(preserveScroll);
        }
        else
        {
            ShowInfo("File not found", "The current file no longer exists.", InfoBarSeverity.Warning);
        }
    }

    private async void OnOpenClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        await PickAndOpenFileAsync();
    }

    private async Task PickAndOpenFileAsync()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(_owner));
        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".markdown");
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await OpenFileAsync(file.Path);
        }
    }

    private async void OnReloadClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        await ReloadAsync(preserveScroll: false);
    }

    private void OnRecentClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        HideWorkspace();
        var page = CreateRecentFilesPage();
        ShowWorkspace("Recent files", "Open a recent Markdown document or choose another file.", page);
    }

    private async void OnSearchClicked(object sender, RoutedEventArgs e)
    {
        await StartFindAsync();
    }

    private async Task StartFindAsync()
    {
        if (_viewMode == DocumentViewMode.Raw)
        {
            RawMarkdownTextBox.Focus(FocusState.Programmatic);
            return;
        }

        if (!await EnsureWebViewAsync())
        {
            return;
        }

        if (ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        var options = ReaderWebView.CoreWebView2.Environment.CreateFindOptions();
        options.FindTerm = string.Empty;
        options.ShouldHighlightAllMatches = true;
        options.SuppressDefaultFindDialog = false;
        await ReaderWebView.CoreWebView2.Find.StartAsync(options);
    }

    private void OnToggleTocClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        SetTocVisible(TocPane.Visibility != Visibility.Visible && CanShowToc());
    }

    private void OnZoomOutClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);
        _ = ApplyZoomAsync();
    }

    private void OnZoomInClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        _zoomFactor = Math.Min(2.5, _zoomFactor + 0.1);
        _ = ApplyZoomAsync();
    }

    private async Task ApplyZoomAsync()
    {
        if (ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        await ReaderWebView.CoreWebView2.ExecuteScriptAsync(
            $"document.documentElement.style.zoom = '{_zoomFactor.ToString(CultureInfo.InvariantCulture)}'");
    }

    private async void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        var rawSelection = RawMarkdownTextBox.SelectedText;
        if ((_viewMode == DocumentViewMode.Raw || RawMarkdownTextBox.FocusState != FocusState.Unfocused) &&
            !string.IsNullOrEmpty(rawSelection))
        {
            CopyText(rawSelection);
            return;
        }

        if (ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        var json = await ReaderWebView.CoreWebView2.ExecuteScriptAsync("window.getSelection().toString()");
        var selectedText = JsonSerializer.Deserialize<string>(json);
        if (!string.IsNullOrEmpty(selectedText))
        {
            CopyText(selectedText);
        }
    }

    private async void OnOpenExternalClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        if (_currentFilePath is not null)
        {
            var launched = await _externalEditor.OpenWithPickerAsync(_currentFilePath);
            if (!launched)
            {
                ShowInfo("Editor unavailable", "Windows could not find an app to edit this Markdown file.", InfoBarSeverity.Warning);
            }
        }
    }

    private void OnFocusClicked(object sender, RoutedEventArgs e)
    {
        SetFocusMode(FocusButton.IsChecked == true);
    }

    private void OnExitFocusClicked(object sender, RoutedEventArgs e)
    {
        SetFocusMode(false);
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        HideWorkspace();
        var page = new SettingsPage();
        page.Load(_readerSettings, _settingsService.LoadRecentFiles());
        page.SettingsChanged += OnSettingsPageChanged;
        page.ClearRecentFilesClicked += (_, _) =>
        {
            _settingsService.ClearRecentFiles();
            RefreshRecentFiles();
        };

        _activeSettingsPage = page;
        ShowWorkspace("Settings", "Reader preferences and local app data.", page);
    }

    private async void OnSettingsPageChanged(object? sender, EventArgs e)
    {
        if (sender is not SettingsPage page)
        {
            return;
        }

        page.ApplyTo(_readerSettings);
        _settingsService.Save(_readerSettings);
        if (_currentFilePath is not null)
        {
            await RenderCurrentFileAsync(preserveScroll: true);
        }
        else
        {
            UpdateStats(null);
        }
    }

    private void OnAboutClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        HideWorkspace();
        ShowWorkspace(
            "About Openza Reader",
            "Version, project links, license, and security information.",
            new AboutPage());
    }

    private void OnCloseWorkspaceClicked(object sender, RoutedEventArgs e)
    {
        HideWorkspace();
    }

    private void OnPreviewModeClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        SetViewMode(DocumentViewMode.Preview);
    }

    private void OnRawModeClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        SetViewMode(DocumentViewMode.Raw);
    }

    private void OnSideBySideModeClicked(object sender, RoutedEventArgs e)
    {
        KeepToolbarLabelsVisible();
        SetViewMode(DocumentViewMode.SideBySide);
    }

    private void OnReaderCommandBarClosed(object sender, object e)
    {
        KeepToolbarLabelsVisible();
    }

    private async void OnTocItemInvoked(object? sender, TocEntryViewModel tocItem)
    {
        if (_currentHtmlUri is null || ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        var builder = new UriBuilder(_currentHtmlUri) { Fragment = Uri.EscapeDataString(tocItem.Id) };
        ReaderWebView.CoreWebView2.Navigate(builder.Uri.AbsoluteUri);
        await Task.CompletedTask;
    }

    private async void OnRecentFileInvoked(object? sender, string path)
    {
        await OpenFileAsync(path);
    }

    private async void OnEmptyStateOpenFileRequested(object? sender, EventArgs e)
    {
        await PickAndOpenFileAsync();
    }

    private void OnEmptyStateClearRecentFilesRequested(object? sender, EventArgs e)
    {
        _settingsService.ClearRecentFiles();
        RefreshRecentFiles();
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var firstMarkdown = items.OfType<StorageFile>()
            .FirstOrDefault(file => file.FileType.Equals(".md", StringComparison.OrdinalIgnoreCase)
                || file.FileType.Equals(".markdown", StringComparison.OrdinalIgnoreCase));
        if (firstMarkdown is not null)
        {
            await OpenFileAsync(firstMarkdown.Path);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width < 900 && TocPane.Visibility == Visibility.Visible)
        {
            SetTocVisible(false);
        }
    }

    private void SetViewMode(DocumentViewMode mode)
    {
        _viewMode = mode;
        SelectViewMode(mode);
        ApplyViewMode();
    }

    private void SelectViewMode(DocumentViewMode mode)
    {
        PreviewModeItem.IsChecked = mode == DocumentViewMode.Preview;
        RawModeItem.IsChecked = mode == DocumentViewMode.Raw;
        SideBySideModeItem.IsChecked = mode == DocumentViewMode.SideBySide;
        ViewModeButton.Label = "View";
        ToolTipService.SetToolTip(ViewModeButton, $"View mode: {ViewModeLabel(mode)}");
    }

    private void ApplyViewMode()
    {
        switch (_viewMode)
        {
            case DocumentViewMode.Raw:
                RawColumn.Width = new GridLength(1, GridUnitType.Star);
                ReaderDividerColumn.Width = new GridLength(0);
                PreviewColumn.Width = new GridLength(0);
                SearchButton.IsEnabled = false;
                TocButton.IsEnabled = false;
                SetTocVisible(false);
                break;
            case DocumentViewMode.SideBySide:
                RawColumn.Width = new GridLength(1, GridUnitType.Star);
                ReaderDividerColumn.Width = new GridLength(1);
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                SearchButton.IsEnabled = true;
                TocButton.IsEnabled = _viewModel.TocItems.Count > 0;
                break;
            default:
                RawColumn.Width = new GridLength(0);
                ReaderDividerColumn.Width = new GridLength(0);
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                SearchButton.IsEnabled = true;
                TocButton.IsEnabled = _viewModel.TocItems.Count > 0;
                break;
        }
    }

    private void SetTocVisible(bool visible)
    {
        var actualVisible = visible && CanShowToc() && _viewModel.TocItems.Count > 0;
        TocPane.Visibility = actualVisible ? Visibility.Visible : Visibility.Collapsed;
        TocColumn.Width = actualVisible ? new GridLength(300) : new GridLength(0);
    }

    private bool CanShowToc() => _viewMode is DocumentViewMode.Preview or DocumentViewMode.SideBySide;

    private void RefreshRecentFiles()
    {
        var recent = _settingsService.LoadRecentFiles();
        _viewModel.SetRecentFiles(recent);
        EmptyState.SetRecentFiles(_viewModel.RecentFiles);
        _activeRecentFilesPage?.SetRecentFiles(_viewModel.RecentFiles);
    }

    private void UpdateStats(DocumentStats? stats)
    {
        if (!_readerSettings.ShowDocumentStats || stats is null)
        {
            DocumentStatsText.Text = string.Empty;
            return;
        }

        var readTime = stats.EstimatedReadMinutes == 1 ? "1 min read" : $"{stats.EstimatedReadMinutes} min read";
        DocumentStatsText.Text = $"{stats.WordCount:n0} words - {readTime} - {stats.HeadingCount:n0} headings";
    }

    private void SetFocusMode(bool enabled)
    {
        if (_isFocusMode == enabled)
        {
            return;
        }

        _isFocusMode = enabled;
        FocusButton.IsChecked = enabled;
        if (enabled)
        {
            HideWorkspace();
        }

        TitleBarRow.Height = enabled ? new GridLength(0) : new GridLength(40);
        ToolbarRow.Height = enabled ? new GridLength(0) : GridLength.Auto;
        InfoBarRow.Height = enabled ? new GridLength(0) : GridLength.Auto;
        AppTitleBar.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        ToolbarHost.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        DocumentInfoBar.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        FocusExitButton.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        SetTocVisible(false);

        if (_owner is MainWindow mainWindow)
        {
            mainWindow.SetImmersiveFocus(enabled);
        }

        if (!enabled)
        {
            SetTocVisible(_viewModel.TocItems.Count > 0 && ActualWidth >= 900 && CanShowToc());
        }
    }

    private void ShowWorkspace(string title, string subtitle, UIElement content)
    {
        SetFocusMode(false);
        PageWorkspaceTitle.Text = title;
        PageWorkspaceSubtitle.Text = subtitle;
        PageWorkspaceContent.Content = content;
        PageWorkspace.Visibility = Visibility.Visible;
        SetTocVisible(false);
    }

    private void HideWorkspace()
    {
        if (_activeSettingsPage is not null)
        {
            _activeSettingsPage.SettingsChanged -= OnSettingsPageChanged;
            _activeSettingsPage = null;
        }

        if (_activeRecentFilesPage is not null)
        {
            _activeRecentFilesPage.OpenFileRequested -= OnEmptyStateOpenFileRequested;
            _activeRecentFilesPage.ClearRecentFilesRequested -= OnEmptyStateClearRecentFilesRequested;
            _activeRecentFilesPage.RecentFileInvoked -= OnRecentFileInvoked;
            _activeRecentFilesPage = null;
        }

        PageWorkspaceContent.Content = null;
        PageWorkspace.Visibility = Visibility.Collapsed;
        SetTocVisible(_viewModel.TocItems.Count > 0 && ActualWidth >= 900 && CanShowToc());
    }

    private ReaderEmptyStateControl CreateRecentFilesPage()
    {
        var page = new ReaderEmptyStateControl();
        page.OpenFileRequested += OnEmptyStateOpenFileRequested;
        page.ClearRecentFilesRequested += OnEmptyStateClearRecentFilesRequested;
        page.RecentFileInvoked += OnRecentFileInvoked;
        page.SetRecentFiles(_viewModel.RecentFiles);
        _activeRecentFilesPage = page;
        return page;
    }

    private void ShowInfo(string title, string message, InfoBarSeverity severity, ButtonBase? actionButton = null)
    {
        DocumentInfoBar.Title = title;
        DocumentInfoBar.Message = message;
        DocumentInfoBar.Severity = severity;
        DocumentInfoBar.ActionButton = actionButton;
        DocumentInfoBar.IsOpen = true;
    }

    private void ShowWebViewRuntimeUnavailable()
    {
        var runtimeButton = new HyperlinkButton
        {
            Content = "Get WebView2 Runtime",
            NavigateUri = new Uri("https://developer.microsoft.com/microsoft-edge/webview2/")
        };

        ShowInfo(
            "WebView2 Runtime unavailable",
            "Install or repair Microsoft Edge WebView2 Runtime to render Markdown preview.",
            InfoBarSeverity.Error,
            runtimeButton);
    }

    private void KeepToolbarLabelsVisible()
    {
        if (_isFocusMode)
        {
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() =>
        {
            ReaderCommandBar.IsSticky = true;
            ReaderCommandBar.IsOpen = true;
        });
    }

    private static string ReaderThemeName(ReaderThemeKind theme)
    {
        return theme switch
        {
            ReaderThemeKind.Light => "light",
            ReaderThemeKind.Dark => "dark",
            ReaderThemeKind.Sepia => "sepia",
            _ => "system"
        };
    }

    private static string ViewModeLabel(DocumentViewMode mode)
    {
        return mode switch
        {
            DocumentViewMode.Raw => "Raw",
            DocumentViewMode.SideBySide => "Side by side",
            _ => "Preview"
        };
    }

    private static void CopyText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
