using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashCardApp.Models;
using FlashCardApp.Services;

namespace FlashCardApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView = null!;

    private readonly DataService _dataService;
    private ObservableCollection<Deck> _decks;

    public MainWindowViewModel()
    {
        // Initialize DataService
        _dataService = new DataService();

        // Load data from file
        _decks = _dataService.Load();

        // Set initial view to DeckList
        GoToDeckList();
    }

    /// <summary>
    /// Navigate to the Deck List view
    /// </summary>
    public void GoToDeckList()
    {
        // Reload data (optional - for fresh data)
        _decks = _dataService.Load();
        
        // Create DeckListViewModel with loaded data and navigation actions
        var deckListViewModel = new DeckListViewModel(
            _decks, 
            _dataService,
            GoToEditor,      // Action for editing/adding decks
            SaveDecks,       // Action for saving after delete
            GoToDeckDetail   // Action for viewing deck details
        );

        CurrentView = deckListViewModel;
    }

    /// <summary>
    /// Navigate to the Editor view
    /// </summary>
    /// <param name="deck">The deck to edit, or null to create a new deck</param>
    public void GoToEditor(Deck? deck)
    {
        var isNewDeck = deck == null;
        
        var editorViewModel = new EditorViewModel(
            deck,
            onSave: () =>
            {
                // If it's a new deck, add it to the collection
                if (isNewDeck)
                {
                    var editor = CurrentView as EditorViewModel;
                    if (editor != null)
                    {
                        _decks.Add(editor.CurrentDeck);
                    }
                }
                
                // Save all decks to file
                SaveDecks();
                
                // Navigate back to deck list
                GoToDeckList();
            },
            onCancel: () =>
            {
                // Just navigate back without saving
                GoToDeckList();
            }
        );

        CurrentView = editorViewModel;
    }

    /// <summary>
    /// Navigate to the Deck Detail view
    /// </summary>
    public void GoToDeckDetail(Deck deck)
    {
        var detailViewModel = new DeckDetailViewModel(
            deck,
            startStudy: GoToStudy,
            goBack: GoToDeckList,
            editDeck: GoToEditor
        );

        CurrentView = detailViewModel;
    }

    /// <summary>
    /// Navigate to the Study view
    /// </summary>
    public void GoToStudy(Deck deck)
    {
        var studyViewModel = new StudyViewModel(
            onFinish: GoToResults,
            onAbort: GoToDeckList
        );
        
        studyViewModel.LoadDeck(deck);
        CurrentView = studyViewModel;
    }

    /// <summary>
    /// Navigate to the Results view
    /// </summary>
    public void GoToResults(Deck deck, StudyStats stats)
    {
        var resultViewModel = new StudyResultViewModel(
            deck,
            stats,
            restartStudy: GoToStudy,
            goHome: GoToDeckList
        );

        CurrentView = resultViewModel;
    }

    /// <summary>
    /// Save all decks to file
    /// </summary>
    private void SaveDecks()
    {
        _dataService.Save(_decks);
    }
}
