using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlashCardApp.Models;

namespace FlashCardApp.Services;

/// <summary>
/// File-based cache for word translations
/// Stores lookup results to reduce API calls
/// </summary>
public class WordCacheService
{
    private readonly string _cacheFilePath;
    private Dictionary<string, CachedWord> _cache = new();
    private bool _isLoaded = false;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);
    private const int MaxEntries = 5000; // safety cap to keep file small

    public WordCacheService()
    {
        // Store cache in app's data directory
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlashCardApp"
        );
        
        // Ensure directory exists
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        _cacheFilePath = Path.Combine(appDataPath, "word_cache.json");
    }

    /// <summary>
    /// Try to get cached word lookup result
    /// </summary>
    public async Task<WordLookupResult?> GetCachedAsync(string word)
    {
        await EnsureLoadedAsync();
        
        var key = word.ToLower().Trim();
        if (_cache.TryGetValue(key, out var cached))
        {
            // Check if cache is still valid (30 days)
            if (DateTime.UtcNow - cached.CachedAt < TimeSpan.FromDays(30))
            {
                return cached.ToLookupResult();
            }
        }
        
        return null;
    }

    /// <summary>
    /// Save word lookup result to cache
    /// </summary>
    public async Task SetCacheAsync(string word, WordLookupResult result)
    {
        if (!result.IsSuccess || result.Definitions.Count == 0)
            return;

        await EnsureLoadedAsync();
        
        var key = word.ToLower().Trim();
        _cache[key] = CachedWord.FromLookupResult(result);
        
        await SaveCacheAsync();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<(int wordCount, long fileSizeKB)> GetStatsAsync()
    {
        await EnsureLoadedAsync();
        
        long fileSize = 0;
        if (File.Exists(_cacheFilePath))
        {
            fileSize = new FileInfo(_cacheFilePath).Length / 1024;
        }
        
        return (_cache.Count, fileSize);
    }

    /// <summary>
    /// Clear all cached words
    /// </summary>
    public async Task ClearCacheAsync()
    {
        _cache.Clear();
        
        if (File.Exists(_cacheFilePath))
        {
            File.Delete(_cacheFilePath);
        }
        
        await Task.CompletedTask;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, CachedWord>>(json) 
                         ?? new Dictionary<string, CachedWord>();
            }
        }
        catch
        {
            // If cache is corrupted, start fresh
            _cache = new Dictionary<string, CachedWord>();
        }

        // Remove expired entries and trim to max size
        var now = DateTime.UtcNow;
        _cache = _cache
            .Where(kvp => now - kvp.Value.CachedAt <= CacheTtl)
            .OrderByDescending(kvp => kvp.Value.CachedAt)
            .Take(MaxEntries)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        _isLoaded = true;
    }

    private async Task SaveCacheAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions
            {
                WriteIndented = false // Compact to save space
            });
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

/// <summary>
/// Cached word data structure
/// </summary>
public class CachedWord
{
    public string Word { get; set; } = string.Empty;
    public List<CachedDefinition> Definitions { get; set; } = new();
    public DateTime CachedAt { get; set; }

    public static CachedWord FromLookupResult(WordLookupResult result)
    {
        var cached = new CachedWord
        {
            Word = result.Word,
            CachedAt = DateTime.UtcNow
        };

        foreach (var def in result.Definitions)
        {
            cached.Definitions.Add(new CachedDefinition
            {
                PartOfSpeech = def.PartOfSpeech,
                ChineseTranslation = def.ChineseTranslation
            });
        }

        return cached;
    }

    public WordLookupResult ToLookupResult()
    {
        var result = new WordLookupResult
        {
            Word = Word,
            IsSuccess = true
        };

        foreach (var def in Definitions)
        {
            result.Definitions.Add(new WordDefinition
            {
                Word = Word,
                PartOfSpeech = def.PartOfSpeech,
                ChineseTranslation = def.ChineseTranslation
            });
        }

        return result;
    }
}

public class CachedDefinition
{
    public string PartOfSpeech { get; set; } = string.Empty;
    public string ChineseTranslation { get; set; } = string.Empty;
}
