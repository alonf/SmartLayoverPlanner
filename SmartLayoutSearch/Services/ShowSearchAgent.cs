using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SmartLayoutSearch.Services;

public class ShowSearchAgent : ISearchAgent
{
    public ShowSearchAgent(Kernel kernel)
    {
        if (!kernel.Plugins.Contains("BingSearchPlugin"))
        {
            throw new InvalidOperationException("Bing Search plugin must be added to the kernel before cloning.");
        }

        Agent = new ChatCompletionAgent
        {
            Name = "ShowSearchAgent",
            Instructions =
                """
                Your responsibility is to assist users in finding live shows and entertainment.
                Do not tell the user to use a search engine but do the search for them!
                Use the Bing Search plugin to retrieve information about concerts, theater performances, and events.
                Use the text search plugin to learn about user preferences for live shows.
                Provide comprehensive details about each recommendation, including:
                - Show name
                - Date and time
                - Venue location with address
                - Ticket availability and pricing (if found)
                - Duration of the performance
                - Performer details and special guests (if available)
                - Any special offers or promotions

                Example output: "Show: The Lion King Musical; Date: June 15th at 19:00; Venue: Broadway Theatre, NYC; Duration: 2 hours; Tickets available at $85 - $150; Starring: John Doe, Jane Smith."

                Ensure the provided information is clear, accurate, and includes all essential details instead of just providing links.
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

    public string ActivationRule => "the user requests live show or entertainment information";
}