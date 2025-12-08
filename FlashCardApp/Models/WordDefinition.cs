using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashCardApp.Models;

/// <summary>
/// Represents a word definition with part of speech and translations
/// </summary>
public partial class WordDefinition : ObservableObject
{
    /// <summary>
    /// The original English word
    /// </summary>
    public string Word { get; set; } = string.Empty;
    
    /// <summary>
    /// Phonetic pronunciation (e.g., /ˈæpəl/)
    /// </summary>
    public string Phonetic { get; set; } = string.Empty;
    
    /// <summary>
    /// Part of speech (e.g., noun, verb, adjective)
    /// </summary>
    public string PartOfSpeech { get; set; } = string.Empty;
    
    /// <summary>
    /// English definition
    /// </summary>
    public string EnglishDefinition { get; set; } = string.Empty;
    
    /// <summary>
    /// Chinese translation
    /// </summary>
    public string ChineseTranslation { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this definition is selected for the card
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
    
    /// <summary>
    /// Short part of speech abbreviation
    /// </summary>
    public string ShortPartOfSpeech => GetShortPartOfSpeech();
    
    /// <summary>
    /// Display text for checkbox: "(n.) 蘋果"
    /// </summary>
    public string DisplayText => $"({ShortPartOfSpeech}) {ChineseTranslation}";
    
    /// <summary>
    /// Simple definition for the card back (Chinese only)
    /// e.g., "(n.) 蘋果"
    /// </summary>
    public string SimpleDefinition => $"({ShortPartOfSpeech}) {ChineseTranslation}";
    
    private string GetShortPartOfSpeech()
    {
        return PartOfSpeech?.ToLower() switch
        {
            "noun" => "n.",
            "verb" => "v.",
            "adjective" => "adj.",
            "adverb" => "adv.",
            "pronoun" => "pron.",
            "preposition" => "prep.",
            "conjunction" => "conj.",
            "interjection" => "interj.",
            "exclamation" => "excl.",
            _ => PartOfSpeech ?? ""
        };
    }
}

/// <summary>
/// Contains all definitions for a word (may have multiple parts of speech)
/// </summary>
public class WordLookupResult
{
    public string Word { get; set; } = string.Empty;
    public string Phonetic { get; set; } = string.Empty;
    public List<WordDefinition> Definitions { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
