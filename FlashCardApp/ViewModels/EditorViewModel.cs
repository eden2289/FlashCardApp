using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;

namespace FlashCardApp.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private Deck _currentDeck;

    [ObservableProperty]
    private string _pageTitle;

    private readonly bool _isNewDeck;
    private readonly Action _onSave;
    private readonly Action _onCancel;

    public EditorViewModel(Deck? deck, Action onSave, Action onCancel)
    {
        _onSave = onSave;
        _onCancel = onCancel;

        if (deck == null)
        {
            // Creating a new deck with one empty flashcard
            _isNewDeck = true;
            _currentDeck = new Deck
            {
                Name = string.Empty,
                Cards = new ObservableCollection<Flashcard>
                {
                    new Flashcard()
                }
            };
            _pageTitle = "Create New Deck";
        }
        else
        {
            // Editing an existing deck
            _isNewDeck = false;
            _currentDeck = deck;
            _pageTitle = "Edit Deck";
        }
    }

    public bool IsNewDeck => _isNewDeck;

    [RelayCommand]
    private void AddCard()
    {
        CurrentDeck.Cards.Add(new Flashcard());
    }

    [RelayCommand]
    private void RemoveCard(Flashcard card)
    {
        if (card != null && CurrentDeck.Cards.Contains(card))
        {
            CurrentDeck.Cards.Remove(card);
        }
    }

    [RelayCommand]
    private void Save()
    {
        // Remove empty cards before saving
        var emptyCards = CurrentDeck.Cards
            .Where(c => string.IsNullOrWhiteSpace(c.Front) && string.IsNullOrWhiteSpace(c.Back))
            .ToList();
        
        foreach (var card in emptyCards)
        {
            CurrentDeck.Cards.Remove(card);
        }

        // If no valid cards exist, don't create the deck - just go back
        if (CurrentDeck.Cards.Count == 0)
        {
            _onCancel?.Invoke();
            return;
        }

        // Validate deck name is not empty
        if (string.IsNullOrWhiteSpace(CurrentDeck.Name))
        {
            CurrentDeck.Name = "未命名單字本";
        }

        // Trigger save callback
        _onSave?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        // Trigger cancel callback (navigate back without saving)
        _onCancel?.Invoke();
    }
}
