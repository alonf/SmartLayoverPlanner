#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0050

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

namespace SmartLayoutSearch.Services;

public static class ChatServiceExtensions
{
    public static IServiceCollection AddChatService(this IServiceCollection services)
    {
        services.AddSingleton<IChatService>(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();

            // Retrieve Azure OpenAI settings from configuration
            var modelId = configuration["AzureOpenAI:ModelId"] ?? throw new ArgumentException("AzureOpenAI:ModelId is missing");
            var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentException("AzureOpenAI:Endpoint is missing");
            var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentException("AzureOpenAI:ApiKey is missing");
            var embeddingModelId = configuration["AzureOpenAI:EmbeddingModelId"] ?? throw new ArgumentException("AzureOpenAI:EmbeddingModelId is missing");
            var bingSearchApiKey = configuration["BingSearch:ApiKey"] ?? throw new ArgumentException("BingSearch:ApiKey is missing");

            var kernel = Kernel.CreateBuilder()
                .AddAzureOpenAITextEmbeddingGeneration(embeddingModelId, endpoint, apiKey)
                .AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey)
                .Build();

            var bingSearch = new BingTextSearch(apiKey: bingSearchApiKey);
            var bingSearchPlugin = bingSearch.CreateWithGetTextSearchResults("BingSearchPlugin");
            kernel.Plugins.Add(bingSearchPlugin);

            ISearchAgent[] searchAgents = new ISearchAgent[]
            {
                new TravelPlannerAgent(kernel),
                new FlightSearchAgent(kernel),
                new AccommodationSearchAgent(kernel),
                new ShowSearchAgent(kernel),
                new RestaurantSearchAgent(kernel),
                new ActivitiesAgent(kernel)
            };

            ILogger<UserPreferenceService> userPreferenceServiceLogger = provider.GetRequiredService<ILogger<UserPreferenceService>>();
            IUserPreferenceService userPreferenceService = new UserPreferenceService(kernel, userPreferenceServiceLogger);

            ILogger<ChatService> chatServiceLogger = provider.GetRequiredService<ILogger<ChatService>>();
            var chatService = new ChatService(kernel, userPreferenceService, chatServiceLogger, searchAgents);
            
            return chatService;
        });
        return services;
    }
}


#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0050
