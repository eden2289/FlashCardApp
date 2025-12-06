using System;
using System.Collections.ObjectModel;

namespace FlashCardApp.Models
{
    // Represents a collection of flashcards (a deck)
    public class Deck
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Name of the deck (e.g., "TOEIC Vocabulary")
        public string Name { get; set; } = "New Deck";

        // ObservableCollection automatically notifies the UI when items are added or removed
        public ObservableCollection<Flashcard> Cards { get; set; } = new ObservableCollection<Flashcard>();
    }
}