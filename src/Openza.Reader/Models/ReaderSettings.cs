namespace Openza.Reader.Models;

public sealed class ReaderSettings
{
    public DocumentViewMode DefaultViewMode { get; set; } = DocumentViewMode.Preview;

    public ReaderThemeKind ReaderTheme { get; set; } = ReaderThemeKind.System;

    public RemoteImagePolicy RemoteImages { get; set; } = RemoteImagePolicy.Allow;

    public bool ShowDocumentStats { get; set; } = true;
}
