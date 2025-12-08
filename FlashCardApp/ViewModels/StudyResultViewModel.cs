using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;

namespace FlashCardApp.ViewModels;

public partial class StudyResultViewModel : ViewModelBase
{
    [ObservableProperty]
    private Deck _deck;

    [ObservableProperty]
    private StudyStats _stats;

    [ObservableProperty]
    private string _durationText = "";

    [ObservableProperty]
    private string _accuracyText = "";

    private readonly Action<Deck> _restartStudy;
    private readonly Action _goHome;

    public StudyResultViewModel(Deck deck, StudyStats stats, Action<Deck> restartStudy, Action goHome)
    {
        _deck = deck;
        _stats = stats;
        _restartStudy = restartStudy;
        _goHome = goHome;

        CalculateDisplayStats();
    }

    // Parameterless constructor for design-time
    public StudyResultViewModel()
    {
        _deck = new Deck { Name = "Sample Deck" };
        _stats = new StudyStats
        {
            TotalCards = 10,
            KnownCards = 8,
            TotalRounds = 2,
            Duration = TimeSpan.FromMinutes(5)
        };
        _restartStudy = _ => { };
        _goHome = () => { };
        CalculateDisplayStats();
    }

    private void CalculateDisplayStats()
    {
        // Format duration
        if (Stats.Duration.TotalMinutes >= 1)
        {
            DurationText = $"{(int)Stats.Duration.TotalMinutes} 分 {Stats.Duration.Seconds} 秒";
        }
        else
        {
            DurationText = $"{Stats.Duration.Seconds} 秒";
        }

        // Calculate accuracy (first round known / total)
        if (Stats.TotalCards > 0)
        {
            var accuracy = (double)Stats.KnownCards / Stats.TotalCards * 100;
            AccuracyText = $"{accuracy:F0}%";
        }
        else
        {
            AccuracyText = "N/A";
        }
    }

    [RelayCommand]
    private void Restart()
    {
        _restartStudy?.Invoke(Deck);
    }

    [RelayCommand]
    private void Home()
    {
        _goHome?.Invoke();
    }
}
