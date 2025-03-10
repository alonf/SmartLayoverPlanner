using System.Text.Json;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001

namespace SmartLayoutSearch.Services;

public class UserPreferenceService : IUserPreferenceService
{
    private const string FilePath = "UserPreferences.json";
    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private IVectorStoreRecordCollection<string, UserPreference>? _preferencesCollection;
    private readonly KernelFunction _extractVitalFactPrompt;
    private readonly ILogger<UserPreferenceService> _logger;


    // ReSharper disable once ConvertToPrimaryConstructor
    public UserPreferenceService(Kernel kernel, ILogger<UserPreferenceService> logger)
    {
        _kernel = kernel;
        _embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _logger = logger;

        _extractVitalFactPrompt = kernel.CreateFunctionFromPrompt(
            """
            Extract a comma-separated list of facts about the user's preferences for flights,
            accommodation, restaurants, shows, and any travel-related preferences from the following input.
            For each fact, indicate the likelihood or no likelihood (e.g., "likely" or "unlikely") in the description.
            Any fact include the likelihood or no likelihood information should be in a separate string.
            Do not provide facts that are one or two words long.
            Create an empty string if you cannot find any important and travel related information.
            
            The prompt is a user prompt that contains a regular chat with the travel agent. 
            There is a good chance that the user do not mention their preferences in the chat,
            so returning and empty string is a valid response.
            Only if the user mentions their preferences, the function should return a comma-separated list of facts.
            
            Example of a possible output:
            The user prefers direct flights, is likely to stay in a 5-star hotel, is unlikely to eat at fast food restaurants,
            The user has visited Paris before, is likely to attend a Broadway show, is unlikely to visit museums, Is like to have a red-eye flight
            
            Do not use the above example as input. Instead, provide the result based on the user's input.

            User Input:
            {{$input}}
            
            """);
    }

    private async Task<JsonDocument?> ReadJsonFileAsync()
    {
        if (!File.Exists(FilePath))
        {
            _logger.LogWarning("File {FilePath} does not exist", FilePath);
            return null;
        }

        var jsonString = await File.ReadAllTextAsync(FilePath);
        
        if (string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("File {FilePath} is empty", FilePath);
            return null;
        }

        return JsonDocument.Parse(jsonString);
    }

    public async Task<IVectorStoreRecordCollection<string, UserPreference>> LoadPreferencesAsync()
    {
        InMemoryVectorStore vectorStore = new();
        _preferencesCollection = vectorStore.GetCollection<string, UserPreference>("preferences");
        await _preferencesCollection.CreateCollectionIfNotExistsAsync();

        var json = await ReadJsonFileAsync();

        if (json == null)
            return _preferencesCollection;

        var preferences = json.Deserialize<List<UserPreference>>();
        if (preferences == null)
            return _preferencesCollection;

        foreach (var pref in preferences)
        {
            pref.DescriptionEmbedding ??= await _embeddingGenerator.GenerateEmbeddingAsync(pref.Description);
            await _preferencesCollection.UpsertAsync(pref);
        }

        return _preferencesCollection;
    }

    private async Task UpdatePreferenceFileAsync(UserPreference newPreference)
    {
        var records = await ReadJsonFileAsync();


        var preferences =
            records == null ? new List<UserPreference>() : records.Deserialize<List<UserPreference>?>() ?? new List<UserPreference>();

        newPreference = newPreference with { DescriptionEmbedding = null };
        preferences.Add(newPreference);

        var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(FilePath, json);
    }

    private async Task CheckAndUpsertPreferenceAsync(UserPreference newPreference, float threshold = 0.8f)
    {
        if (string.IsNullOrWhiteSpace(newPreference.Description) || newPreference.Description.Length < 10)
        {
            _logger.LogWarning("Preference description is too short.");
            return;
        }

        if (_preferencesCollection == null)
        {
            _logger.LogInformation("Loading preferences collection.");
            await LoadPreferencesAsync();
        }

        if (_preferencesCollection == null)
        {
            _logger.LogWarning("Preferences collection is still null after loading.");
            return;
        }

        newPreference.DescriptionEmbedding ??= await _embeddingGenerator.GenerateEmbeddingAsync(newPreference.Description);

        var results = await _preferencesCollection.VectorizedSearchAsync(newPreference.DescriptionEmbedding,
            new VectorSearchOptions { Top = 1 });

        await foreach (var result in results.Results)
        {
            if (result.Score > threshold)
            {
                _logger.LogInformation("Preference already exists with a score of {Score}.", result.Score);
                return; // Preference already exists
            }
        }

        _logger.LogInformation("Upserting new preference.");
        await _preferencesCollection.UpsertAsync(newPreference);

        _logger.LogInformation("Updating preference file with new preference.");
        await UpdatePreferenceFileAsync(newPreference);
    }

    public async Task ExtractForUserPreferenceFactAsync(string userPrompt)
    {
        var context = new KernelArguments()
        {
            ["input"] = userPrompt
        };
        var commaSeparatedFacts = await _extractVitalFactPrompt.InvokeAsync<string>(_kernel, context);
        
        if (string.IsNullOrWhiteSpace(commaSeparatedFacts))
            return;

        var extractedFacts = commaSeparatedFacts.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var extractedFact in extractedFacts)
        {
            if (string.IsNullOrWhiteSpace(extractedFact))
                continue;

            var newPref = new UserPreference { Description = extractedFact };
            await CheckAndUpsertPreferenceAsync(newPref);
        }
    }
}

#pragma warning restore SKEXP0001