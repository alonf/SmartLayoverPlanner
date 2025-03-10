using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SmartLayoverFunctionCall;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var modelId = configuration["AzureOpenAI:ModelId"] ?? throw new ArgumentException("AzureOpenAI:ModelId is missing");
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentException("AzureOpenAI:Endpoint is missing");
var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentException("AzureOpenAI:ApiKey is missing");

// Define the system prompt
const string systemPrompt = "You are an intelligent travel assistant. Use your sources to answer!";

// Create a Kernel and add Azure OpenAI
var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

// Add Airline Plugin
builder.Plugins.AddFromType<AirlineSearchPlugin>();

// Add enhanced JSON logging
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Information)); //try LogLevel.Debug, LogLevel.Trace, LogLevel.Warning, LogLevel.Error

// Build the kernel
var kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Initialize ChatHistory with the system prompt
var chatHistory = new ChatHistory(systemPrompt);

// Enable automatic function calling
var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Function to simulate user input and get assistant response from AzureOpenAI
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

// Simulate the conversation flow
await SimulateConversationAsync("Which airline has flights from London to Seattle?");