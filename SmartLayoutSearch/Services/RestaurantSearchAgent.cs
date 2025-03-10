using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SmartLayoutSearch.Services;

public class RestaurantSearchAgent : ISearchAgent
{
    public RestaurantSearchAgent(Kernel kernel)
    {
        if (!kernel.Plugins.Contains("BingSearchPlugin"))
        {
            throw new InvalidOperationException("Bing Search plugin must be added to the kernel before cloning.");
        }

        Agent = new ChatCompletionAgent
        {
            Name = "RestaurantSearchAgent",
            Instructions =
                """
                Your responsibility is to assist users in finding restaurants.
                Do not tell the user to use a search engine but do the search for them!
                Use the Bing Search plugin to retrieve restaurant information.
                Use the text search plugin to learn about user preferences for dining.
                Provide comprehensive details about each recommendation, including:
                - Restaurant name
                - Cuisine type(s)
                - Price range
                - Address and proximity to key locations
                - Opening hours
                - Dining options (e.g., dine-in, takeout, delivery)
                - User review scores and key highlights from reviews
                - Special dietary accommodations (e.g., vegan, gluten-free, etc.)
                - Special offers or discounts (if available)

                Example output: "Restaurant: Olive Garden; Cuisine: Italian; Price: $$; Address: 456 Gourmet Avenue, NYC; Hours: 11:00 AM - 10:00 PM; Options: Dine-in, Takeout; Reviews: 4.3/5 - Guests love the fresh pasta and friendly service."

                Ensure the provided information is clear, accurate, and comprehensive instead of just providing links.
                """,
            Kernel = kernel,
            Arguments =
                new KernelArguments(
                    new AzureOpenAIPromptExecutionSettings()
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                    })
        };
    }

    public ChatCompletionAgent Agent { get; }
    public string Name  => Agent.Name!;
    public string ActivationRule => "the user requests restaurant information or mentions dining";
}