using CommunityToolkit.Mvvm.ComponentModel;
using FlashCardApp.Services;

namespace FlashCardApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    private readonly DataService _dataService;

    public MainWindowViewModel()
    {
        // Initialize DataService
        _dataService = new DataService();

        // Load data from file
        var decks = _dataService.Load();

        // Create DeckListViewModel with loaded data
        var deckListViewModel = new DeckListViewModel(decks);

        // Set as initial view
        CurrentView = deckListViewModel;
    }
}
