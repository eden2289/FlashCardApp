using System;
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
    private Flashcard? _previewCard;

    [ObservableProperty]
    private bool _isPreviewFlipped;

    [ObservableProperty]
    private int _previewIndex;

    private readonly Action<Deck> _startStudy;
    private readonly Action _goBack;

    public DeckDetailViewModel(Deck deck, Action<Deck> startStudy, Action goBack)
    {
        _currentDeck = deck ?? throw new ArgumentNullException(nameof(deck));
        _startStudy = startStudy;
        _goBack = goBack;
        
        // Set first card as preview
        var validCards = deck.Cards.Where(c => 
            !string.IsNullOrWhiteSpace(c.Front) || !string.IsNullOrWhiteSpace(c.Back)).ToList();
        if (validCards.Count > 0)
        {
            PreviewCard = validCards[0];
            PreviewIndex = 0;
        }
    }

    // Parameterless constructor for design-time
    public DeckDetailViewModel()
    {
        _currentDeck = new Deck { Name = "Sample Deck" };
        _startStudy = _ => { };
        _goBack = () => { };
    }

    public string PreviewProgress => PreviewCard != null 
        ? $"{PreviewIndex + 1} / {CurrentDeck.ValidCardCount}" 
        : "0 / 0";

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
    private void FlipPreview()
    {
        IsPreviewFlipped = !IsPreviewFlipped;
    }

    [RelayCommand]
    private void PreviousCard()
    {
        var validCards = CurrentDeck.Cards.Where(c => 
            !string.IsNullOrWhiteSpace(c.Front) || !string.IsNullOrWhiteSpace(c.Back)).ToList();
        
        if (validCards.Count == 0) return;
        
        PreviewIndex = (PreviewIndex - 1 + validCards.Count) % validCards.Count;
        PreviewCard = validCards[PreviewIndex];
        IsPreviewFlipped = false;
        OnPropertyChanged(nameof(PreviewProgress));
    }

    [RelayCommand]
    private void NextCard()
    {
        var validCards = CurrentDeck.Cards.Where(c => 
            !string.IsNullOrWhiteSpace(c.Front) || !string.IsNullOrWhiteSpace(c.Back)).ToList();
        
        if (validCards.Count == 0) return;
        
        PreviewIndex = (PreviewIndex + 1) % validCards.Count;
        PreviewCard = validCards[PreviewIndex];
        IsPreviewFlipped = false;
        OnPropertyChanged(nameof(PreviewProgress));
    }
}
