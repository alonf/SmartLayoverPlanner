using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SmartLayoutSearch.Services;

public class FlightSearchAgent : ISearchAgent
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public FlightSearchAgent(Kernel kernel)
    {
        // Ensure Bing Search plugin is available in the cloned kernel
        if (!kernel.Plugins.Contains("BingSearchPlugin"))
        {
            throw new InvalidOperationException("Bing Search plugin must be added to the kernel before cloning.");
        }

        Agent = new ChatCompletionAgent
        {
            Name = "FlightSearchAgent",
            Instructions =
                """"
                Your responsibility is to assist users with finding flights by performing a search for flight information and returning specific flight details. 
                Do not instruct the user or recommend to use a search engine or viewing a site. You must search and extract the details by yourself and provide:
                - Airline name
                - Departure time
                - Arrival time
                - Estimated flight duration
                - Layover details (if applicable)
                
                Example output: "Airline: Delta Airlines; Departure: TLV at 08:00; Arrival: NYC at 16:00; Layover: JFK for 2 hours; Total flight duration: 10 hours."
                Use the Bing Search plugin to retrieve this information and provide it directly.
                
                Use the text search plugin to learn about user preferences for flights.
                """",
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

    public string ActivationRule => " the user requests flight information or mentions travel";
}