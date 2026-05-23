using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Openza.Reader.ViewModels;

namespace Openza.Reader.Controls;

public sealed partial class TocPaneControl : UserControl
{
    public event EventHandler<TocEntryViewModel>? TocItemInvoked;

    public TocPaneControl()
    {
        InitializeComponent();
    }

    public void SetItems(IEnumerable<TocEntryViewModel> items)
    {
        TocList.ItemsSource = items;
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TocEntryViewModel item)
        {
            TocItemInvoked?.Invoke(this, item);
        }
    }
}

