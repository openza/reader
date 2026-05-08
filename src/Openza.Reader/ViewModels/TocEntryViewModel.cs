using Microsoft.UI.Xaml;
using Openza.Reader.Models;

namespace Openza.Reader.ViewModels;

public sealed record TocEntryViewModel(string Id, string Title, int Level)
{
    public Thickness IndentMargin => new(Math.Max(0, Level - 1) * 14, Level == 1 ? 9 : 7, 8, Level == 1 ? 9 : 7);

    public double FontSize => Level == 1 ? 14.5 : 14;

    public static TocEntryViewModel FromModel(TocItem item)
    {
        return new TocEntryViewModel(item.Id, item.Title, item.Level);
    }
}
