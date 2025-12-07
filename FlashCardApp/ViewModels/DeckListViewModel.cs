using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;

namespace FlashCardApp.ViewModels;

public partial class DeckListViewModel : ViewModelBase
{
    public ObservableCollection<Deck> Decks { get; }

    public DeckListViewModel(ObservableCollection<Deck> decks)
    {
        Decks = decks;
    }

    [RelayCommand]
    private void AddDeck()
    {
        var newDeck = new Deck
        {
            Name = "新單字本"
        };
        Decks.Add(newDeck);
    }
}
