using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Openza.Reader.Services;
using Openza.Reader.Shell;
using Windows.Graphics;

namespace Openza.Reader;

public sealed partial class MainWindow : Window
{
    private readonly ReaderShell _shell;
    private bool _isImmersiveFocus;
    private AppWindowPresenterKind _presenterKindBeforeFocus = AppWindowPresenterKind.Overlapped;
    private OverlappedPresenterState _overlappedStateBeforeFocus = OverlappedPresenterState.Maximized;
    private RectInt32 _windowBoundsBeforeFocus;
    private bool _hasWindowBoundsBeforeFocus;

    public MainWindow(
        MarkdownRenderer renderer,
        HtmlShellBuilder shellBuilder,
        TempHtmlDocumentStore documentStore,
        ExternalEditorService externalEditor,
        AppSettingsService settings)
    {
        AppLog.Write("MainWindow constructor before InitializeComponent");
        InitializeComponent();
        AppLog.Write("MainWindow InitializeComponent complete");

        _shell = new ReaderShell(this, renderer, shellBuilder, documentStore, externalEditor, settings);
        Root.Children.Add(_shell);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_shell.TitleBarElement);
        TryEnableMica();
        TryMaximizeOnStartup();

        Closed += (_, _) => _shell.Close();
    }

    public Task OpenFileAsync(string filePath) => _shell.OpenFileAsync(filePath);

    internal void SetImmersiveFocus(bool enabled)
    {
        if (_isImmersiveFocus == enabled)
        {
            return;
        }

        _isImmersiveFocus = enabled;
        try
        {
            if (enabled)
            {
                CaptureWindowStateBeforeFocus();
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                return;
            }

            RestoreWindowStateBeforeFocus();
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
        }
    }

    private void TryEnableMica()
    {
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
        }
    }

    private void CaptureWindowStateBeforeFocus()
    {
        _presenterKindBeforeFocus = AppWindow.Presenter.Kind;
        _hasWindowBoundsBeforeFocus = false;

        if (AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        _overlappedStateBeforeFocus = presenter.State;
        _windowBoundsBeforeFocus = new RectInt32(
            AppWindow.Position.X,
            AppWindow.Position.Y,
            AppWindow.Size.Width,
            AppWindow.Size.Height);
        _hasWindowBoundsBeforeFocus = true;
    }

    private void RestoreWindowStateBeforeFocus()
    {
        AppWindow.SetPresenter(_presenterKindBeforeFocus);
        if (_presenterKindBeforeFocus != AppWindowPresenterKind.Overlapped ||
            AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        presenter.Restore();
        if (_hasWindowBoundsBeforeFocus &&
            _overlappedStateBeforeFocus == OverlappedPresenterState.Restored)
        {
            AppWindow.MoveAndResize(_windowBoundsBeforeFocus);
            return;
        }

        if (_overlappedStateBeforeFocus == OverlappedPresenterState.Maximized)
        {
            presenter.Maximize();
        }
        else if (_overlappedStateBeforeFocus == OverlappedPresenterState.Minimized)
        {
            presenter.Minimize();
        }
    }

    private void TryMaximizeOnStartup()
    {
        try
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
        catch (Exception exception)
        {
            AppLog.Write(exception);
        }
    }
}
