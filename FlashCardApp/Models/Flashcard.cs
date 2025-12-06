using System;

namespace FlashCardApp.Models
{
    // Represents a single flashcard
    public class Flashcard
    {
        // Unique ID for identification
        public Guid Id { get; set; } = Guid.NewGuid();

        // The front side of the card (e.g., the word or question)
        public string Front { get; set; } = string.Empty;

        // The back side of the card (e.g., the definition or answer)
        public string Back { get; set; } = string.Empty;
    }
}