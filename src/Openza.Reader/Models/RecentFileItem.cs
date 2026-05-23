namespace Openza.Reader.Models;

public sealed record RecentFileItem(string Path)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    public string FolderPath => System.IO.Path.GetDirectoryName(Path) ?? Path;
}
