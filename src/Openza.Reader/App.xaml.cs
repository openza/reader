using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Openza.Reader.Services;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Openza.Reader;

public partial class App : Application
{
    private readonly MarkdownRenderer _renderer = new();
    private readonly HtmlShellBuilder _shellBuilder = new();
    private readonly TempHtmlDocumentStore _documentStore =
        new(ApplicationData.Current.LocalCacheFolder.Path);
    private readonly ExternalEditorService _externalEditor = new();
    private readonly AppSettingsService _settings = new();
    private readonly List<MainWindow> _windows = [];
    private readonly DispatcherQueue _dispatcherQueue;

    public App()
    {
        AppLog.Write("App constructor");
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UnhandledException += (_, args) => AppLog.Write(args.Exception);
        AppInstance.GetCurrent().Activated += OnActivated;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            AppLog.Write("OnLaunched");
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            if (!TryOpenFileActivation(activationArgs))
            {
                OpenWindow(null);
            }
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
            throw;
        }
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            if (!TryOpenFileActivation(args))
            {
                OpenWindow(null);
            }
        }))
        {
            AppLog.Write("Failed to enqueue app activation on UI dispatcher.");
        }
    }

    private bool TryOpenFileActivation(AppActivationArguments args)
    {
        if (args.Kind != ExtendedActivationKind.File || args.Data is not IFileActivatedEventArgs fileArgs)
        {
            return false;
        }

        var opened = false;
        foreach (var file in fileArgs.Files.OfType<StorageFile>())
        {
            OpenWindow(file.Path);
            opened = true;
        }

        return opened;
    }

    private void OpenWindow(string? filePath)
    {
        AppLog.Write($"OpenWindow filePath='{filePath ?? "<none>"}'");
        var window = new MainWindow(_renderer, _shellBuilder, _documentStore, _externalEditor, _settings);
        _windows.Add(window);
        window.Closed += (_, _) =>
        {
            AppLog.Write("Window closed");
            _windows.Remove(window);
        };
        window.Activate();
        AppLog.Write($"Window activated. Window count={_windows.Count}");

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _ = OpenFileForWindowAsync(window, filePath);
        }
    }

    private static async Task OpenFileForWindowAsync(MainWindow window, string filePath)
    {
        try
        {
            await window.OpenFileAsync(filePath);
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
        }
    }
}
