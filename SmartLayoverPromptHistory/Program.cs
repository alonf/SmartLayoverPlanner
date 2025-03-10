// 1. Import necessary namespaces
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// 2. Load configuration from appsettings.json
var configuration = LoadConfiguration();

// Retrieve Azure OpenAI settings from configuration
var modelId = configuration["AzureOpenAI:ModelId"] ?? throw new ArgumentException("AzureOpenAI:ModelId is missing");
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentException("AzureOpenAI:Endpoint is missing");
var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentException("AzureOpenAI:ApiKey is missing");

// 3. Define the system prompt
const string systemPrompt = "You are an intelligent travel assistant. Be polite, provide short and accurate answers.";

// 4. Create a Kernel instance and configure AzureOpenAI
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Add enterprise components (logging)
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Information)); //try LogLevel.Debug, LogLevel.Trace, LogLevel.Warning, LogLevel.Error

// Build the kernel
Kernel kernel = builder.Build();

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// 5. Initialize ChatHistory with the system prompt
var chatHistory = new ChatHistory(systemPrompt);

// 6. Function to simulate user input and get assistant response from AzureOpenAI
async Task SimulateConversationAsync(string userMessage)
{
    chatHistory.AddUserMessage(userMessage);

    // Display user message
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"User: {userMessage}");
    Console.ResetColor();

    try
    {
        var result = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            kernel: kernel);

        // Display assistant response
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Assistant > " + result);
        Console.ResetColor();

        // Add assistant message to history
        chatHistory.AddMessage(result.Role, result.Content ?? string.Empty);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
    }

    await Task.Delay(1000); // Simulate realistic response time
}

// 7. Simulate the conversation flow
await SimulateConversationAsync("How long is the flight from London to NYC?");
await SimulateConversationAsync("And to Seattle?");

// Utility: Load configuration from appsettings.json
IConfiguration LoadConfiguration() =>
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.local.json", optional: true) // Excluded from source control
        .AddEnvironmentVariables()
        .Build();