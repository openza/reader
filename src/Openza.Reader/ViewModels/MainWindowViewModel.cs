using CommunityToolkit.Mvvm.ComponentModel;
using Openza.Reader.Models;
using System.Collections.ObjectModel;

namespace Openza.Reader.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Title { get; set; }

    public ObservableCollection<TocEntryViewModel> TocItems { get; } = [];

    public void SetDocument(MarkdownRenderResult result)
    {
        Title = result.Title;
        TocItems.Clear();
        foreach (var item in result.TocItems)
        {
            TocItems.Add(TocEntryViewModel.FromModel(item));
        }
    }
}
