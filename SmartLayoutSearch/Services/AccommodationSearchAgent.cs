using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SmartLayoutSearch.Services;

public class AccommodationSearchAgent : ISearchAgent
{
    public AccommodationSearchAgent(Kernel kernel)
    {
        if (!kernel.Plugins.Contains("BingSearchPlugin"))
        {
            throw new InvalidOperationException("Bing Search plugin must be added to the kernel before cloning.");
        }

        Agent = new ChatCompletionAgent
        {
            Name = "AccommodationSearchAgent",
            Instructions =
                """
                Your responsibility is to assist users in finding accommodations.
                Do not tell the user to use a search engine but do the search for them!
                Use the Bing Search plugin to retrieve accommodation information.
                Use the text search plugin to learn about user preferences for accommodations.
                Provide comprehensive details about each recommendation, including:
                - Accommodation name
                - Type (e.g., Hotel, Airbnb, Hostel, etc.)
                - Price range
                - Address and proximity to key locations
                - Available amenities (e.g., Wi-Fi, parking, breakfast, etc.)
                - User review scores and key highlights from reviews
                - Special offers or discounts (if available)

                Example output: "Accommodation: The Grand Hotel; Type: Hotel; Price: $150 - $200 per night; Address: 123 Main Street, NYC; Amenities: Free Wi-Fi, Pool, Gym; Reviews: 4.5/5 - Guests love the spacious rooms and friendly staff."

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
    public string Name => Agent.Name!;
    public string ActivationRule => "the user requests hotel, Airbnb, or lodging details ";
}