using CommunityToolkit.Mvvm.ComponentModel;
using Openza.Reader.Models;
using System.Collections.ObjectModel;

namespace Openza.Reader.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Title { get; set; }

    public ObservableCollection<TocEntryViewModel> TocItems { get; } = [];

    public ObservableCollection<RecentFileItem> RecentFiles { get; } = [];

    public void SetDocument(MarkdownRenderResult result)
    {
        Title = result.Title;
        TocItems.Clear();
        foreach (var item in result.TocItems)
        {
            TocItems.Add(TocEntryViewModel.FromModel(item));
        }
    }

    public void SetRecentFiles(IEnumerable<RecentFileItem> files)
    {
        RecentFiles.Clear();
        foreach (var file in files)
        {
            RecentFiles.Add(file);
        }
    }
}
