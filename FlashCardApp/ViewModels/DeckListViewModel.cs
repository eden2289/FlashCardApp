using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;
using FlashCardApp.Services;

namespace FlashCardApp.ViewModels;

public partial class DeckListViewModel : ViewModelBase
{
    public ObservableCollection<Deck> Decks { get; }

    private readonly DataService _dataService;
    private readonly Action<Deck?> _navigateToEditor;
    private readonly Action _saveDecks;
    private readonly Action<Deck>? _navigateToDetail;

    public DeckListViewModel(
        ObservableCollection<Deck> decks, 
        DataService dataService,
        Action<Deck?> navigateToEditor,
        Action saveDecks,
        Action<Deck>? navigateToDetail = null)
    {
        Decks = decks;
        _dataService = dataService;
        _navigateToEditor = navigateToEditor;
        _saveDecks = saveDecks;
        _navigateToDetail = navigateToDetail;
    }

    // Parameterless constructor for design-time
    public DeckListViewModel()
    {
        Decks = new ObservableCollection<Deck>();
        _dataService = new DataService();
        _navigateToEditor = _ => { };
        _saveDecks = () => { };
        _navigateToDetail = null;
    }

    [RelayCommand]
    private void AddDeck()
    {
        // Navigate to editor with null (create new deck)
        _navigateToEditor?.Invoke(null);
    }

    [RelayCommand]
    private void EditDeck(Deck deck)
    {
        // Navigate to editor with existing deck
        _navigateToEditor?.Invoke(deck);
    }

    [RelayCommand]
    private void DeleteDeck(Deck deck)
    {
        if (deck != null && Decks.Contains(deck))
        {
            Decks.Remove(deck);
            // Persist the deletion
            _saveDecks?.Invoke();
        }
    }

    [RelayCommand]
    private void ViewDeck(Deck deck)
    {
        // Navigate to deck detail view
        _navigateToDetail?.Invoke(deck);
    }
}
