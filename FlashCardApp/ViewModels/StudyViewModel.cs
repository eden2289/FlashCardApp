using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;

namespace FlashCardApp.ViewModels;

/// <summary>
/// Record to track study actions for undo functionality
/// </summary>
public record StudyAction(Flashcard Card, bool WasUnknown);

/// <summary>
/// Statistics for a study session
/// </summary>
public class StudyStats
{
    public int TotalCards { get; set; }
    public int KnownCards { get; set; }
    public int UnknownCards { get; set; }
    public int TotalRounds { get; set; }
    public TimeSpan Duration { get; set; }
}

public partial class StudyViewModel : ViewModelBase
{
    private Queue<Flashcard> _studyQueue = new();
    private List<Flashcard> _unknownCards = new();
    private List<Flashcard> _knownCards = new();
    private Stack<StudyAction> _history = new();
    private Stopwatch _stopwatch = new();
    private int _currentRound = 1;
    private int _totalCardsInSession;
    private readonly Random _rng = Random.Shared;

    private readonly Action<Deck, StudyStats> _onFinish;
    private readonly Action _onAbort;

    [ObservableProperty]
    private Deck? _currentDeck;

    [ObservableProperty]
    private Flashcard? _currentCard;

    [ObservableProperty]
    private bool _isFlipped;

    [ObservableProperty]
    private string _progress = "";

    [ObservableProperty]
    private string _roundInfo = "";

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _isReviewingMissed;

    public StudyViewModel(Action<Deck, StudyStats> onFinish, Action onAbort)
    {
        _onFinish = onFinish;
        _onAbort = onAbort;
    }

    // Parameterless constructor for design-time
    public StudyViewModel()
    {
        _onFinish = (_, _) => { };
        _onAbort = () => { };
    }

    /// <summary>
    /// Load a deck and prepare for studying
    /// </summary>
    public void LoadDeck(Deck deck)
    {
        CurrentDeck = deck;
        
        // Get valid cards only
        var validCards = deck.Cards
            .Where(c => !string.IsNullOrWhiteSpace(c.Front) || !string.IsNullOrWhiteSpace(c.Back))
            .ToList();

        var shuffled = validCards.OrderBy(_ => _rng.Next()).ToList();

        _studyQueue = new Queue<Flashcard>(shuffled);
        _unknownCards.Clear();
        _knownCards.Clear();
        _history.Clear();
        _currentRound = 1;
        _totalCardsInSession = shuffled.Count;
        IsReviewingMissed = false;

        if (_totalCardsInSession == 0)
        {
            Progress = "沒有可用卡片";
            RoundInfo = "第 0 輪";
            CurrentCard = null;
            _stopwatch.Reset();
            return;
        }

        _stopwatch.Restart();
        
        UpdateProgress();
        NextCard();
    }

    /// <summary>
    /// Move to the next card in the queue
    /// </summary>
    private void NextCard()
    {
        IsFlipped = false;

        if (_studyQueue.Count > 0)
        {
            CurrentCard = _studyQueue.Dequeue();
            UpdateProgress();
            CanUndo = _history.Count > 0;
        }
        else
        {
            // Queue is empty, check for unknown cards
            if (_unknownCards.Count > 0)
            {
                // Move unknown cards back to queue for another round
                _currentRound++;
                IsReviewingMissed = true;
                RoundInfo = $"複習錯誤卡片 (第 {_currentRound} 輪)";

                var shuffled = _unknownCards.OrderBy(_ => _rng.Next()).ToList();
                _studyQueue = new Queue<Flashcard>(shuffled);
                _unknownCards.Clear();
                _history.Clear();
                CanUndo = false;

                CurrentCard = _studyQueue.Dequeue();
                UpdateProgress();
            }
            else
            {
                // All cards mastered, finish session
                FinishStudy();
            }
        }
    }

    /// <summary>
    /// Update progress display
    /// </summary>
    private void UpdateProgress()
    {
        var remaining = _studyQueue.Count + (CurrentCard != null ? 1 : 0);
        Progress = $"剩餘: {remaining} 張";
        
        if (!IsReviewingMissed)
        {
            RoundInfo = $"第 {_currentRound} 輪";
        }
    }

    /// <summary>
    /// Finish the study session
    /// </summary>
    private void FinishStudy()
    {
        _stopwatch.Stop();
        CurrentCard = null;

        var stats = new StudyStats
        {
            TotalCards = _totalCardsInSession,
            KnownCards = _knownCards.Count,
            UnknownCards = 0, // All mastered at end
            TotalRounds = _currentRound,
            Duration = _stopwatch.Elapsed
        };

        if (CurrentDeck != null)
        {
            _onFinish?.Invoke(CurrentDeck, stats);
        }
    }

    [RelayCommand]
    private void SwipeRight()
    {
        // Card is known
        if (CurrentCard == null) return;

        _history.Push(new StudyAction(CurrentCard, false));
        _knownCards.Add(CurrentCard);
        NextCard();
    }

    [RelayCommand]
    private void SwipeLeft()
    {
        // Card is unknown
        if (CurrentCard == null) return;

        _history.Push(new StudyAction(CurrentCard, true));
        _unknownCards.Add(CurrentCard);
        NextCard();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_history.Count == 0 || CurrentCard == null) return;

        var lastAction = _history.Pop();

        // Remove from the appropriate list
        if (lastAction.WasUnknown)
        {
            _unknownCards.Remove(lastAction.Card);
        }
        else
        {
            _knownCards.Remove(lastAction.Card);
        }

        // Push current card back to front of queue
        var tempList = _studyQueue.ToList();
        tempList.Insert(0, CurrentCard);
        _studyQueue = new Queue<Flashcard>(tempList);

        // Set the undone card as current
        CurrentCard = lastAction.Card;
        IsFlipped = false;
        UpdateProgress();
        CanUndo = _history.Count > 0;
    }

    [RelayCommand]
    private void Flip()
    {
        IsFlipped = !IsFlipped;
    }

    [RelayCommand]
    private void Abort()
    {
        _stopwatch.Stop();
        _onAbort?.Invoke();
    }
}
