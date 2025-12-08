using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashCardApp.Models;
using FlashCardApp.Services;

namespace FlashCardApp.ViewModels;

/// <summary>
/// ViewModel wrapper for a Flashcard that adds dictionary lookup functionality
/// </summary>
public partial class FlashcardEditorViewModel : ObservableObject
{
    private readonly DictionaryService _dictionaryService;
    
    public Flashcard Card { get; }

    [ObservableProperty]
    private string _front = string.Empty;

    [ObservableProperty]
    private string _back = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showSuggestions;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<WordDefinition> _suggestions = new();

    /// <summary>
    /// Check if any definition is selected
    /// </summary>
    public bool HasSelection => Suggestions.Any(s => s.IsSelected);

    public FlashcardEditorViewModel(Flashcard card, DictionaryService dictionaryService)
    {
        Card = card;
        _dictionaryService = dictionaryService;
        _front = card.Front;
        _back = card.Back;
    }

    partial void OnFrontChanged(string value)
    {
        Card.Front = value;
        // Hide suggestions when user types
        ShowSuggestions = false;
        ErrorMessage = string.Empty;
    }

    partial void OnBackChanged(string value)
    {
        Card.Back = value;
    }

    [RelayCommand]
    private async Task LookupWordAsync()
    {
        if (string.IsNullOrWhiteSpace(Front))
        {
            ErrorMessage = "請先輸入單字";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        ShowSuggestions = false;
        Suggestions.Clear();

        try
        {
            var result = await _dictionaryService.LookupWordAsync(Front.Trim());

            if (result.IsSuccess && result.Definitions.Count > 0)
            {
                foreach (var def in result.Definitions)
                {
                    // Subscribe to selection changes
                    def.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(WordDefinition.IsSelected))
                        {
                            OnPropertyChanged(nameof(HasSelection));
                        }
                    };
                    Suggestions.Add(def);
                }
                ShowSuggestions = true;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Confirm selected definitions and combine them
    /// </summary>
    [RelayCommand]
    private void ConfirmSelection()
    {
        var selectedDefs = Suggestions.Where(s => s.IsSelected).ToList();
        if (selectedDefs.Count == 0) return;

        // Update front with the word
        Front = selectedDefs.First().Word;
        
        // Combine all selected definitions (Chinese only)
        // e.g., "(n.) 蘋果; (v.) 吃蘋果"
        Back = string.Join("; ", selectedDefs.Select(d => d.SimpleDefinition));
        
        ShowSuggestions = false;
        Suggestions.Clear();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var def in Suggestions)
        {
            def.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var def in Suggestions)
        {
            def.IsSelected = false;
        }
    }

    [RelayCommand]
    private void HideSuggestions()
    {
        ShowSuggestions = false;
        Suggestions.Clear();
    }
}
