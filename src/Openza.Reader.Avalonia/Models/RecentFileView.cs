namespace Openza.Reader.Avalonia.Models;

public sealed record RecentFileView(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);

    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    public override string ToString() => $"{Name}  —  {Directory}";
}
