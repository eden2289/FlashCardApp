using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;

namespace FlashCardApp.ViewModels;

public partial class DeckDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private Deck _currentDeck;
    
    [ObservableProperty]
    private ObservableCollection<Flashcard> _validCards = new();
    
    [ObservableProperty]
    private bool _hasCards;

    private readonly Action<Deck> _startStudy;
    private readonly Action _goBack;
    private readonly Action<Deck>? _editDeck;

    public DeckDetailViewModel(Deck deck, Action<Deck> startStudy, Action goBack, Action<Deck>? editDeck = null)
    {
        _currentDeck = deck ?? throw new ArgumentNullException(nameof(deck));
        _startStudy = startStudy;
        _goBack = goBack;
        _editDeck = editDeck;
        
        // Set valid cards for horizontal scroll view
        var cards = deck.Cards.Where(c => 
            !string.IsNullOrWhiteSpace(c.Front) || !string.IsNullOrWhiteSpace(c.Back)).ToList();
        ValidCards = new ObservableCollection<Flashcard>(cards);
        HasCards = ValidCards.Count > 0;
    }

    // Parameterless constructor for design-time
    public DeckDetailViewModel()
    {
        _currentDeck = new Deck { Name = "Sample Deck" };
        _startStudy = _ => { };
        _goBack = () => { };
        _editDeck = _ => { };
    }

    public string PreviewProgress => ValidCards.Count > 0 
        ? $"共 {ValidCards.Count} 張卡片" 
        : "尚無卡片";

    [RelayCommand]
    private void StartStudy()
    {
        if (CurrentDeck.ValidCardCount > 0)
        {
            _startStudy?.Invoke(CurrentDeck);
        }
    }

    [RelayCommand]
    private void Back()
    {
        _goBack?.Invoke();
    }

    [RelayCommand]
    private void EditDeck()
    {
        _editDeck?.Invoke(CurrentDeck);
    }
}
