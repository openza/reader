using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Openza.Reader.Models;
using Openza.Reader.Services;
using Openza.Reader.ViewModels;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Openza.Reader;

public sealed partial class MainWindow : Window
{
    private readonly MarkdownRenderer _renderer;
    private readonly HtmlShellBuilder _shellBuilder;
    private readonly TempHtmlDocumentStore _documentStore;
    private readonly ExternalEditorService _externalEditor;
    private readonly MainWindowViewModel _viewModel = new();
    private readonly DispatcherQueueTimer _reloadTimer;
    private FileSystemWatcher? _watcher;
    private string? _currentFilePath;
    private Uri? _currentHtmlUri;
    private double _zoomFactor = 1.0;

    public MainWindow(
        MarkdownRenderer renderer,
        HtmlShellBuilder shellBuilder,
        TempHtmlDocumentStore documentStore,
        ExternalEditorService externalEditor)
    {
        _renderer = renderer;
        _shellBuilder = shellBuilder;
        _documentStore = documentStore;
        _externalEditor = externalEditor;

        AppLog.Write("MainWindow constructor before InitializeComponent");
        InitializeComponent();
        AppLog.Write("MainWindow InitializeComponent complete");

        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
        }

        _reloadTimer = DispatcherQueue.CreateTimer();
        _reloadTimer.Interval = TimeSpan.FromMilliseconds(350);
        _reloadTimer.Tick += async (_, _) =>
        {
            _reloadTimer.Stop();
            await ReloadAsync(preserveScroll: true);
        };

        SizeChanged += OnSizeChanged;
        Activated += async (_, _) =>
        {
            AppLog.Write("MainWindow activated event");
            try
            {
                await EnsureWebViewAsync();
            }
            catch (Exception exception)
            {
                AppLog.Write(exception);
            }
        };
        Closed += (_, _) => _watcher?.Dispose();
    }

    public async Task OpenFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        _currentFilePath = filePath;
        Title = $"{Path.GetFileName(filePath)} - Openza Reader";
        await RenderCurrentFileAsync(preserveScroll: false);
        StartWatcher(filePath);
    }

    private async Task EnsureWebViewAsync()
    {
        if (ReaderWebView.CoreWebView2 is not null)
        {
            return;
        }

        await ReaderWebView.EnsureCoreWebView2Async();
        var core = ReaderWebView.CoreWebView2 ?? throw new InvalidOperationException("WebView2 failed to initialize.");
        var settings = core.Settings;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;
        settings.IsScriptEnabled = true;
        settings.IsStatusBarEnabled = false;
        settings.AreDevToolsEnabled = false;
        core.NavigationStarting += OnNavigationStarting;
    }

    private async Task RenderCurrentFileAsync(bool preserveScroll)
    {
        if (_currentFilePath is null)
        {
            return;
        }

        await EnsureWebViewAsync();
        var scrollState = preserveScroll ? await CaptureScrollStateAsync() : null;
        var markdown = await File.ReadAllTextAsync(_currentFilePath);
        var result = _renderer.Render(markdown, _currentFilePath);
        var html = _shellBuilder.Build(result, Path.GetFileName(_currentFilePath));
        _currentHtmlUri = await _documentStore.WriteAsync(_currentFilePath, html);

        _viewModel.SetDocument(result);
        TocList.ItemsSource = _viewModel.TocItems;
        EmptyState.Visibility = Visibility.Collapsed;
        SetTocVisible(_viewModel.TocItems.Count > 0 && Bounds.Width >= 900);
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
                SetTocVisible(false);
                return;
            }

            _reloadTimer.Stop();
            _reloadTimer.Start();
        });
    }

    private async Task ReloadAsync(bool preserveScroll)
    {
        if (_currentFilePath is not null && File.Exists(_currentFilePath))
        {
            await RenderCurrentFileAsync(preserveScroll);
        }
    }

    private async void OnOpenClicked(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
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
        await ReloadAsync(preserveScroll: false);
    }

    private void OnToggleTocClicked(object sender, RoutedEventArgs e)
    {
        SetTocVisible(TocPane.Visibility != Visibility.Visible);
    }

    private void OnZoomOutClicked(object sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);
        _ = ApplyZoomAsync();
    }

    private void OnZoomInClicked(object sender, RoutedEventArgs e)
    {
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
            $"document.documentElement.style.zoom = '{_zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}'");
    }

    private async void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        if (ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        var json = await ReaderWebView.CoreWebView2.ExecuteScriptAsync("window.getSelection().toString()");
        var selectedText = JsonSerializer.Deserialize<string>(json);
        if (string.IsNullOrEmpty(selectedText))
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(selectedText);
        Clipboard.SetContent(package);
    }

    private void OnOpenExternalClicked(object sender, RoutedEventArgs e)
    {
        if (_currentFilePath is not null)
        {
            _externalEditor.Open(_currentFilePath);
        }
    }

    private async void OnTocItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not TocEntryViewModel tocItem || _currentHtmlUri is null || ReaderWebView.CoreWebView2 is null)
        {
            return;
        }

        var builder = new UriBuilder(_currentHtmlUri) { Fragment = Uri.EscapeDataString(tocItem.Id) };
        ReaderWebView.CoreWebView2.Navigate(builder.Uri.AbsoluteUri);
        await Task.CompletedTask;
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

    private async void OnSizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        if (args.Size.Width < 900 && TocPane.Visibility == Visibility.Visible)
        {
            SetTocVisible(false);
        }

        await Task.CompletedTask;
    }

    private void SetTocVisible(bool visible)
    {
        TocPane.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        TocColumn.Width = visible ? new GridLength(300) : new GridLength(0);
    }
}
