using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlashCardApp.Views;

public partial class DeckListView : UserControl
{
    public DeckListView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
