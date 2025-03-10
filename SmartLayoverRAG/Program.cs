using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using SmartLayoverRAG;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var modelId = configuration["AzureOpenAI:ModelId"] ?? throw new ArgumentException("AzureOpenAI:ModelId is missing");
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentException("AzureOpenAI:Endpoint is missing");
var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentException("AzureOpenAI:ApiKey is missing");
var embeddingModelId = configuration["AzureOpenAI:EmbeddingModelId"] ?? throw new ArgumentException("AzureOpenAI:EmbeddingModelId is missing");

// Create Kernel Builder
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embeddingModelId, endpoint, apiKey);

// Add enhanced JSON logging
builder.Services.AddLogging(services =>
        services.AddConsole().SetMinimumLevel(LogLevel.Information));

// Build the Kernel
var kernel = builder.Build();

// Initialize the memory store with the new approach
// Initialize in-memory vector store
var vectorStore = new InMemoryVectorStore();
var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

var flightCollection = vectorStore.GetCollection<string, FlightData>("flightData");
await flightCollection.CreateCollectionIfNotExistsAsync();

// Create a text search instance
var textSearch = new VectorStoreTextSearch<FlightData>(flightCollection, embeddingGenerator);

// Create a plugin from the text search instance
var searchPlugin = textSearch.CreateWithGetTextSearchResults("SearchPlugin");

kernel.Plugins.Add(searchPlugin);

// Load and process flight data
await LoadFlightDataToMemoryAsync();

// Setup chat
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
var chatHistory = new ChatHistory(
    """
    You are a flight information assistant. 
    Use the information in your memory to answer questions about flight routes, airlines, flight destinations, and durations.
    You can query the memory for multiple entries and provide a list of flight destinations.
    When you don't have the exact information, say you don't know.
    Be concise and friendly in your responses.
    """);

// Enable memory search in the execution settings
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    TopP = 0.5,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Simulate conversation
await SimulateConversationAsync("Which airlines fly from London to NYC?");

await SimulateConversationAsync("What's the flight duration from Paris to Rome?");

await SimulateConversationAsync("What's the best airline for Seattle to Tokyo?");

await SimulateConversationAsync("Provide a complete list of flight destinations from Athens");

await SimulateConversationAsync("Find the shortest flight duration destinations from Athens");


async Task LoadFlightDataToMemoryAsync()
{
    Console.WriteLine("Loading flight data to memory...");

    // Read the flight data from the file
    string jsonData = await File.ReadAllTextAsync("FlightData.json");
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    var flightDataList = JsonSerializer.Deserialize<List<FlightData>>(jsonData, options);

    if (flightDataList == null || !flightDataList.Any())
    {
        Console.WriteLine("No flight data found!");
        return;
    }

    // Add each flight data item to memory
    foreach (var flightData in flightDataList)
    {
        // Create a unique ID for this memory entry
        string id = $"flight-{flightData.Route.Replace(" ", "-").ToLower()}";

        // Set properties on the FlightData object
        flightData.Id = id;
        flightData.Description = $"Flight from {flightData.Route} operated by {string.Join(", ", flightData.Airlines)}. " +
                               $"Flight duration is {flightData.Duration}. Best airline for this route is {flightData.BestAirline}.";

        // Generate embedding for the description
        flightData.DescriptionEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(flightData.Description);

        // Add the FlightData object directly to the vector database
        await flightCollection.UpsertAsync(flightData);

        Console.WriteLine($"Added to memory: {flightData.Route}");
    }
    Console.WriteLine("Flight data loaded to memory successfully.");
}

async Task SimulateConversationAsync(string userMessage)
{
    chatHistory.AddUserMessage(userMessage);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"User: {userMessage}");
    Console.ResetColor();

    var result = await chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        executionSettings: executionSettings,
        kernel: kernel);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Assistant > " + result);
    Console.ResetColor();

    chatHistory.AddMessage(result.Role, result.Content ?? string.Empty);
}

#pragma warning restore SKEXP0001

#pragma warning restore SKEXP0010
