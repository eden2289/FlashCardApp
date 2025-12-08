using System;
using System.Collections.ObjectModel;
using System.Linq;

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

        /// <summary>
        /// Count of valid cards (cards with non-empty Front or Back)
        /// </summary>
        public int ValidCardCount => Cards.Count(c => 
            !string.IsNullOrWhiteSpace(c.Front) || !string.IsNullOrWhiteSpace(c.Back));
    }
}