using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SmartLayoutSearch.Services;

public class ActivitiesAgent : ISearchAgent
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ActivitiesAgent(Kernel kernel)
    {
        // Ensure Bing Search plugin is available in the cloned kernel
        if (!kernel.Plugins.Contains("BingSearchPlugin"))
        {
            throw new InvalidOperationException("Bing Search plugin must be added to the kernel before cloning.");
        }

        Agent = new ChatCompletionAgent
        {
            Name = "ActivitiesAgent",
            Instructions =
                """"
                Your responsibility is to assist users with finding outdoor and cultural activities, including:
                - Hiking trails and natural parks
                - Road trip routes and scenic drives
                - Museums, art galleries, and historical sites
                - Sightseeing opportunities and landmarks
                - City tours and hop-on/hop-off services
                - Local festivals and events
                - Family-friendly activities and attractions
                
                Do not instruct users to use a search engine or visit websites - perform the search and provide comprehensive information directly.
                
                For each activity recommendation, provide:
                - Name and type of activity
                - Location and how to get there
                - Estimated duration and best time to visit
                - Difficulty level (for hikes/physical activities)
                - Admission costs or fees (if applicable)
                - Notable features or highlights
                - Visitor ratings and reviews (when available)
                - Tips for the best experience
                
                Example output: "Activity: Angel's Landing Trail; Type: Hiking; Location: Zion National Park, Utah; Duration: 4-5 hours; Difficulty: Challenging; Highlights: Stunning views of Zion Canyon, chain section near summit; Reviews: 4.8/5 - Visitors praise the breathtaking vistas; Tip: Start early to avoid crowds and heat."
                
                Use the Bing Search plugin to retrieve detailed and accurate information.
                Use the text search plugin to learn about user preferences for activities and attractions.
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

    public string ActivationRule => " the user requests information about hiking, road trips, parks, museums, sightseeing, tours, attractions or recreational activities";
}
