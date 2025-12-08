using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlashCardApp.Views;

public partial class DeckDetailView : UserControl
{
    public DeckDetailView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
