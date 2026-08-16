using Avalonia;

namespace Openza.Reader.Avalonia.Models;

public sealed record TocItemView(string Id, string Title, int Level)
{
    public Thickness Margin => new(Math.Max(0, Level - 1) * 14, 5, 0, 5);
}
