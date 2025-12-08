using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using FlashCardApp.Models;

namespace FlashCardApp.Services;

/// <summary>
/// Service to look up English words and get definitions with Chinese translations
/// Uses Datamuse API (word lookup) + Google Translate (translation)
/// </summary>
public class DictionaryService
{
    private readonly HttpClient _httpClient;
    private readonly WordCacheService _cacheService;
    private static readonly RequestRateLimiter _rateLimiter = new();
    
    // Datamuse API - free, no key required, large vocabulary
    private const string DatamuseApiUrl = "https://api.datamuse.com/words";

    // Common word translations for accuracy (cached)
    private static readonly Dictionary<string, Dictionary<string, string>> CommonTranslations = new()
    {
        { "fine", new Dictionary<string, string> {
            { "adjective", "好的、優良的" },
            { "noun", "罰款" },
            { "verb", "處以罰款" },
            { "adverb", "很好地" }
        }},
        { "run", new Dictionary<string, string> {
            { "verb", "跑、運行" },
            { "noun", "跑步、運行" }
        }},
        { "light", new Dictionary<string, string> {
            { "noun", "光、燈" },
            { "adjective", "輕的、明亮的" },
            { "verb", "點燃" }
        }},
        { "book", new Dictionary<string, string> {
            { "noun", "書、書籍" },
            { "verb", "預訂" }
        }},
        { "play", new Dictionary<string, string> {
            { "verb", "玩、播放" },
            { "noun", "戲劇、遊戲" }
        }},
        { "watch", new Dictionary<string, string> {
            { "verb", "觀看、注視" },
            { "noun", "手錶" }
        }},
        { "change", new Dictionary<string, string> {
            { "verb", "改變" },
            { "noun", "變化、零錢" }
        }},
        { "present", new Dictionary<string, string> {
            { "noun", "禮物、現在" },
            { "adjective", "出席的、現在的" },
            { "verb", "呈現、贈送" }
        }},
        { "right", new Dictionary<string, string> {
            { "adjective", "正確的、右邊的" },
            { "noun", "權利、右邊" },
            { "adverb", "正確地" }
        }},
        { "left", new Dictionary<string, string> {
            { "adjective", "左邊的" },
            { "noun", "左邊" },
            { "verb", "離開（過去式）" }
        }}
    };

    // Offline fallback dictionary for common polysemy words (avoid API if possible)
    private static readonly Dictionary<string, Dictionary<string, string>> OfflineFallback = new()
    {
        { "fine", new() {{"adjective","好的、優良的"},{"noun","罰款"},{"verb","處以罰款"},{"adverb","很好地"}} },
        { "run", new() {{"verb","跑、運行"},{"noun","跑步、運行"}} },
        { "light", new() {{"noun","光、燈"},{"adjective","輕的、明亮的"},{"verb","點燃"}} },
        { "book", new() {{"noun","書、書籍"},{"verb","預訂"}} },
        { "play", new() {{"verb","玩、播放"},{"noun","戲劇、遊戲"}} },
        { "watch", new() {{"verb","觀看、注視"},{"noun","手錶"}} },
        { "change", new() {{"verb","改變"},{"noun","變化、零錢"}} },
        { "present", new() {{"noun","禮物、現在"},{"adjective","出席的、現在的"},{"verb","呈現、贈送"}} },
        { "right", new() {{"adjective","正確的、右邊的"},{"noun","權利、右邊"},{"adverb","正確地"}} },
        { "left", new() {{"adjective","左邊的"},{"noun","左邊"},{"verb","離開（過去式）"}} },
        { "bank", new() {{"noun","銀行、河岸"},{"verb","存款"}} },
        { "spring", new() {{"noun","春天、彈簧、泉水"},{"verb","彈跳、湧出"}} },
        { "match", new() {{"noun","比賽、火柴、匹配"},{"verb","匹配、相符"}} },
        { "bow", new() {{"noun","弓、鞠躬"},{"verb","鞠躬、彎曲"}} },
        { "lead", new() {{"verb","帶領"},{"noun","鉛、領先"}} },
        { "close", new() {{"verb","關閉"},{"adjective","近的、親密的"}} },
        { "order", new() {{"noun","順序、訂單、命令"},{"verb","訂購、命令"}} },
        { "park", new() {{"noun","公園"},{"verb","停車"}} },
        { "coach", new() {{"noun","教練"},{"verb","訓練、指導"}} },
        { "train", new() {{"noun","火車"},{"verb","訓練"}} },
        { "bear", new() {{"noun","熊"},{"verb","忍受、攜帶"}} },
        { "firm", new() {{"noun","公司"},{"adjective","堅固的"}} },
        { "kind", new() {{"noun","種類"},{"adjective","仁慈的"}} },
        { "mean", new() {{"verb","意味著"},{"adjective","刻薄的、平均的"}} },
        { "fair", new() {{"adjective","公平的、晴朗的"},{"noun","展覽、博覽會"}} },
        { "letter", new() {{"noun","信件、字母"}} },
        { "date", new() {{"noun","日期、約會、棗子"},{"verb","約會、註明日期"}} },
        { "file", new() {{"noun","檔案、文件"},{"verb","歸檔、提出(申請)"}} },
        { "apple", new() {{"noun","蘋果"}} },
        { "orange", new() {{"noun","橘子、橙色"}} }
    };

    public DictionaryService()
    {
        // Reuse a single HttpClient instance for all calls to avoid socket exhaustion
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FlashCardApp/1.0");
        _cacheService = new WordCacheService();
    }

    /// <summary>
    /// Look up a word and get Chinese translations
    /// </summary>
    public async Task<WordLookupResult> LookupWordAsync(string word)
    {
        var result = new WordLookupResult { Word = word };

        if (string.IsNullOrWhiteSpace(word))
        {
            result.IsSuccess = false;
            result.ErrorMessage = "請輸入單字";
            return result;
        }

        var cleanWord = word.Trim().ToLower();

        // Check cache first
        var cachedResult = await _cacheService.GetCachedAsync(cleanWord);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // Offline fallback dictionary (no network, zero cost)
        var offline = GetOfflineResult(cleanWord);
        if (offline != null)
        {
            await _cacheService.SetCacheAsync(cleanWord, offline);
            return offline;
        }

        try
        {
            // Step 1: Verify word exists using Datamuse (spell check)
            var wordExists = await VerifyWordExistsAsync(cleanWord);
            if (!wordExists)
            {
                // Try to get spelling suggestions
                var suggestions = await GetSpellingSuggestionsAsync(cleanWord);
                if (suggestions.Count > 0)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"找不到 \"{cleanWord}\"，您是否要找：{string.Join(", ", suggestions.Take(3))}？";
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "找不到此單字";
                }
                return result;
            }

            // Step 2: Get parts of speech using Datamuse metadata
            var partsOfSpeech = await GetPartsOfSpeechAsync(cleanWord);
            
            // If no POS found, default to treating as noun
            if (partsOfSpeech.Count == 0)
            {
                partsOfSpeech.Add("noun");
            }

            // Step 3: Create definitions and translate for each part of speech
            foreach (var pos in partsOfSpeech)
            {
                // First check if we have a cached translation for this word+POS
                string chineseTranslation;
                if (CommonTranslations.TryGetValue(cleanWord, out var posTranslations) &&
                    posTranslations.TryGetValue(pos, out var cachedTranslation))
                {
                    chineseTranslation = cachedTranslation;
                }
                else
                {
                    // Fallback to API translation with context
                    var translationQuery = GetTranslationQuery(cleanWord, pos);
                    chineseTranslation = await TranslateToChineseAsync(translationQuery);
                    chineseTranslation = CleanTranslation(chineseTranslation, pos);
                }
                
                result.Definitions.Add(new WordDefinition
                {
                    Word = cleanWord,
                    PartOfSpeech = pos,
                    ChineseTranslation = chineseTranslation,
                    EnglishDefinition = ""
                });
            }

            result.IsSuccess = result.Definitions.Count > 0;
            
            // Save to cache for future use
            if (result.IsSuccess)
            {
                await _cacheService.SetCacheAsync(cleanWord, result);
            }
            
            return result;
        }
        catch (TaskCanceledException)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "查詢超時，請檢查網路連線";
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"查詢失敗: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Verify if a word exists in English dictionary using Datamuse
    /// </summary>
    private async Task<bool> VerifyWordExistsAsync(string word)
    {
        try
        {
            await _rateLimiter.WaitAsync();
            // Use "sp" (spelled like) with exact match
            var url = $"{DatamuseApiUrl}?sp={Uri.EscapeDataString(word)}&max=1";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var firstWord = doc.RootElement[0];
                if (firstWord.TryGetProperty("word", out var wordProp))
                {
                    return wordProp.GetString()?.ToLower() == word.ToLower();
                }
            }
            
            return false;
        }
        catch
        {
            return true; // Assume exists on error to allow translation
        }
    }

    /// <summary>
    /// Get spelling suggestions for a misspelled word
    /// </summary>
    private async Task<List<string>> GetSpellingSuggestionsAsync(string word)
    {
        var suggestions = new List<string>();
        
        try
        {
            await _rateLimiter.WaitAsync();
            // Use "sp" with wildcard for similar words
            var url = $"{DatamuseApiUrl}?sp={Uri.EscapeDataString(word)}*&max=5";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return suggestions;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("word", out var wordProp))
                    {
                        var suggestion = wordProp.GetString();
                        if (!string.IsNullOrEmpty(suggestion))
                        {
                            suggestions.Add(suggestion);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore errors for suggestions
        }
        
        return suggestions;
    }

    /// <summary>
    /// Get parts of speech for a word using Datamuse metadata
    /// </summary>
    private async Task<List<string>> GetPartsOfSpeechAsync(string word)
    {
        var partsOfSpeech = new List<string>();
        
        try
        {
            await _rateLimiter.WaitAsync();
            // Use "md=p" to get parts of speech metadata
            var url = $"{DatamuseApiUrl}?sp={Uri.EscapeDataString(word)}&md=p&max=1";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return partsOfSpeech;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var firstWord = doc.RootElement[0];
                if (firstWord.TryGetProperty("tags", out var tagsProp) && 
                    tagsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in tagsProp.EnumerateArray())
                    {
                        var tagValue = tag.GetString();
                        if (string.IsNullOrEmpty(tagValue)) continue;
                        
                        // Datamuse returns tags like "n" for noun, "v" for verb, etc.
                        var pos = tagValue switch
                        {
                            "n" => "noun",
                            "v" => "verb",
                            "adj" => "adjective",
                            "adv" => "adverb",
                            "prep" => "preposition",
                            "conj" => "conjunction",
                            "pron" => "pronoun",
                            "interj" => "interjection",
                            _ => null
                        };
                        
                        if (pos != null && !partsOfSpeech.Contains(pos))
                        {
                            partsOfSpeech.Add(pos);
                        }
                    }
                }
            }
        }
        catch
        {
            // Return empty list on error
        }
        
        return partsOfSpeech;
    }

    /// <summary>
    /// Create a translation query with context for better accuracy
    /// </summary>
    private string GetTranslationQuery(string word, string partOfSpeech)
    {
        // Add context to help translation API understand the part of speech
        return partOfSpeech.ToLower() switch
        {
            "noun" => $"the {word}", // "the fine" → 罰款
            "verb" => $"to {word}", // "to fine" → 罰款（動詞）
            "adjective" => $"a {word} day", // "a fine day" → 美好的一天
            "adverb" => $"doing {word}", // "doing fine" → 做得好
            _ => word
        };
    }

    /// <summary>
    /// Clean up the translation by removing context words
    /// </summary>
    private string CleanTranslation(string translation, string partOfSpeech)
    {
        if (string.IsNullOrEmpty(translation))
            return translation;

        // Remove common prefixes/suffixes added for context
        var cleaned = translation
            .Replace("該", "")
            .Replace("的東西", "")
            .Replace("東西", "")
            .Replace("做得", "")
            .Replace("做它", "")
            .Replace("一個", "")
            .Replace("一天", "")
            .Replace("天", "")
            .Replace("做", "")
            .Trim();

        return string.IsNullOrEmpty(cleaned) ? translation : cleaned;
    }

    private async Task<string> TranslateToChineseAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        try
        {
            await _rateLimiter.WaitAsync();
            // Use Google Translate (free endpoint)
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=zh-TW&dt=t&q={Uri.EscapeDataString(text)}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return text;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            // Google returns nested arrays: [[["翻譯結果","original",...],...]...]
            if (doc.RootElement.ValueKind == JsonValueKind.Array &&
                doc.RootElement.GetArrayLength() > 0 &&
                doc.RootElement[0].ValueKind == JsonValueKind.Array &&
                doc.RootElement[0].GetArrayLength() > 0 &&
                doc.RootElement[0][0].ValueKind == JsonValueKind.Array &&
                doc.RootElement[0][0].GetArrayLength() > 0)
            {
                return doc.RootElement[0][0][0].GetString() ?? text;
            }

            return text;
        }
        catch
        {
            return text; // Fallback to original on error
        }
    }

    /// <summary>
    /// Return offline dictionary result if available
    /// </summary>
    private WordLookupResult? GetOfflineResult(string word)
    {
        if (OfflineFallback.TryGetValue(word, out var posDict))
        {
            var result = new WordLookupResult
            {
                Word = word,
                IsSuccess = true
            };

            foreach (var kvp in posDict)
            {
                result.Definitions.Add(new WordDefinition
                {
                    Word = word,
                    PartOfSpeech = kvp.Key,
                    ChineseTranslation = kvp.Value,
                    EnglishDefinition = string.Empty
                });
            }

            return result;
        }

        return null;
    }
}

/// <summary>
/// Simple sliding-window rate limiter to avoid hitting free API limits
/// </summary>
internal class RequestRateLimiter
{
    private readonly Queue<DateTime> _timestamps = new();
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    private const int MaxRequestsPerWindow = 60; // 60 req / minute
    private readonly object _lock = new();

    public async Task WaitAsync()
    {
        while (true)
        {
            TimeSpan? delay = null;

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                // Drop expired entries
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count < MaxRequestsPerWindow)
                {
                    _timestamps.Enqueue(now);
                    return;
                }

                var oldest = _timestamps.Peek();
                delay = _window - (now - oldest);
            }

            if (delay.HasValue && delay.Value > TimeSpan.Zero)
            {
                await Task.Delay(delay.Value);
            }
        }
    }
}
