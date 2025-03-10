using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SmartLayoutSearch.Services;

public class TravelPlannerAgent : ISearchAgent
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelPlannerAgent(Kernel kernel)
    {
        Agent = new ChatCompletionAgent
        {
            Name = "TravelPlannerAgent",
            Instructions =
                """
                Your responsibility is to plan the user's travel itinerary.
                You can use Markdown tables to provide a summary view.
                You can use QuickChart Markdown Links to create a chart. The Chart line and data colors must be bright since the background is dark, create the graph grid with a light color.
                There are four search agents that you can delegate to:
                - AccommodationSearchAgent
                - FlightSearchAgent
                - RestaurantSearchAgent
                - ShowSearchAgent
                
                The user request may involve finding accommodations, flights, restaurants, or shows.
                Analyze the request, create a plan and response with the main goal and the instruction for each agent.
                
                Goal: The user request ... (e.g. to plan a trip to Paris)
                The instructions include:
                    - the order in which the agents should be called.
                    - Date and time details for each search agent. For example if the flight arrival date is known, provide it to the accommodation agent.
                    - Location details for each search agent. For example if the user is flying to a specific city, provide it to the restaurant agent.
                    - User preferences for each search agent. For example if the user prefers a specific cuisine, provide it to the restaurant agent.
                 
                 You will be called whenever there is a request for travel planning, include intermediary steps to plan the travel.
                 You must check dates and time and make sure that the user's request is feasible. Update the time and date for each search agent accordingly.
                 
                 Example output:
                 Goal: The user request to plan a trip from NYC to Paris on 12/12/2025. The user prefers to stay in a 5-star hotel and eat at a Michelin-starred restaurant. The user ask for a concert.
                 Instructions:
                 - Flight: Search for a flight that [date, time, origin, destination, and any other required detail]. you must provide these result details:
                    * airline
                    * frequent flyer club name
                    * cost
                    * take of and landing times
                    * flight duration
                    * any additional comments
                 - Accommodation: Find a [hotel, apartment, airbnb] in [city, neighborhood] on the [date] and optional arrival time. you must provide these result details:
                    * hotel name
                    * cost
                    * location
                    * amenities
                    * any additional comments
                 - Restaurant: Recommend a restaurant in [location] on [date and time]. The user prefers [cuisine]. you must provide these result details:
                    * restaurant name
                    * cost
                    * location
                    * cuisine
                    * any additional comments
                 - Concert: Find a show or event in [location] on [date and time]. you must provide these result details:
                    * show name
                    * cost
                    * location
                    * time
                    * any additional comments
                 
                 Example:
                 User request: "I want to plan a trip to Paris on 12/12/2025. I want to stay in a 5-star hotel and eat at a Michelin-starred restaurant. I also want to attend a concert."
                 Output:
                 Goal: The user request to plan a trip from NYC to Paris on 12/12/2025. The user prefers to stay in a 5-star hotel and eat at a Michelin-starred restaurant. The user ask for a concert.
                 Instructions:
                 - Flight: Search for a flight from NYC to Paris on 12/12/2025. you must provide these result details:
                    * airline
                    * frequent flyer club name
                    * cost
                    * take of and landing times
                    * flight duration
                    * any additional comments
                - Accommodation: Find a hotel in Paris on 12/12/2025. you must provide these result details:
                    * hotel name
                    * cost
                    * location
                    * amenities
                    * any additional comments
                - Restaurant: Recommend a Michelin-starred restaurant in Paris on 12/12/2025. The user prefers French cuisine. you must provide these result details:
                    * restaurant name
                    * cost
                    * location
                    * cuisine
                    * any additional comments
                - Show: Find a concert in Paris on 12/12/2025 evening. you must provide these result details:
                    * show name
                    * cost
                    * location
                    * time
                    * any additional comments
                 
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
    public string ActivationRule => " there is a need to plan the travel ";
}