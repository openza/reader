namespace Openza.Reader.Avalonia.Models;

public enum ReaderViewMode
{
    Preview,
    Raw,
    SideBySide
}

public enum ReaderColorTheme
{
    System,
    Light,
    Dark,
    Sepia
}

public sealed class ReaderPreferences
{
    public ReaderViewMode ViewMode { get; set; } = ReaderViewMode.Preview;

    public ReaderColorTheme Theme { get; set; } = ReaderColorTheme.System;

    public bool AllowRemoteImages { get; set; } = true;

    public bool ShowDocumentStats { get; set; } = true;

    public List<string> RecentFiles { get; set; } = [];
}
