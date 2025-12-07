using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using FlashCardApp.Models;

namespace FlashCardApp.Services
{
    public class DataService
    {
        private const string FileName = "flashcards.json";

        /// <summary>
        /// Serializes and saves the deck collection to file
        /// </summary>
        public void Save(IEnumerable<Deck> decks)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(decks, options);
                File.WriteAllText(FileName, json);
            }
            catch (IOException ex)
            {
                // Log or handle IO exceptions
                Console.WriteLine($"Error saving file: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"Error serializing data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads and deserializes the deck collection from file
        /// </summary>
        public ObservableCollection<Deck> Load()
        {
            try
            {
                // If file doesn't exist, return empty collection
                if (!File.Exists(FileName))
                {
                    return new ObservableCollection<Deck>();
                }

                string json = File.ReadAllText(FileName);
                
                // Handle empty file
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new ObservableCollection<Deck>();
                }

                var decks = JsonSerializer.Deserialize<List<Deck>>(json);
                return decks != null 
                    ? new ObservableCollection<Deck>(decks) 
                    : new ObservableCollection<Deck>();
            }
            catch (IOException ex)
            {
                // Log or handle IO exceptions, return empty collection
                Console.WriteLine($"Error reading file: {ex.Message}");
                return new ObservableCollection<Deck>();
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing exceptions, return empty collection
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                return new ObservableCollection<Deck>();
            }
            catch (Exception ex)
            {
                // Handle other exceptions, return empty collection
                Console.WriteLine($"Error loading data: {ex.Message}");
                return new ObservableCollection<Deck>();
            }
        }
    }
}
