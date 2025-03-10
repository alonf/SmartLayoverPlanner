#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;

namespace SmartLayoutSearch.Services;

public class ChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly ILogger _logger;
    private bool _isInitialized;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly List<ChatMessageContent> _chatMessages = new();

    private readonly IUserPreferenceService _userPreferenceService;
    private readonly ISearchAgent[] _searchAgents;

    private AgentGroupChat? _chatGroup;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ChatService(Kernel kernel, IUserPreferenceService userPreferenceService, ILogger<ChatService> logger,
        ISearchAgent[] searchAgents)
    {
        _kernel = kernel;
        _logger = logger;
        _userPreferenceService = userPreferenceService;
        kernel.GetRequiredService<IChatCompletionService>();
        _searchAgents = searchAgents;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogInformation("ChatService is already initialized.");
            return;
        }

        _isInitialized = true;
        _logger.LogInformation("Initializing ChatService...");

        var embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var preferences = await _userPreferenceService.LoadPreferencesAsync();
        _logger.LogInformation("User preferences loaded.");

        // Create a text search instance
        var textSearch = new VectorStoreTextSearch<UserPreference>(preferences, embeddingGenerator);

        // Create a plugin from the text search instance
        var searchPlugin = textSearch.CreateWithGetTextSearchResults("SearchPlugin");

        _kernel.Plugins.Add(searchPlugin);
        _logger.LogInformation("Search plugin added to kernel.");

        // Initialize the agent group
        _chatGroup = new AgentGroupChat(_searchAgents.Select(a => a.Agent).ToArray<Agent>())
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new KernelFunctionSelectionStrategy(SelectionFunction, _kernel)
                {
                    InitialAgent = _searchAgents.First().Agent,
                    HistoryReducer = new ChatHistoryTruncationReducer(3),
                    HistoryVariableName = "lastmessage",
                    ResultParser = ResultParse//(result) => result.GetValue<string>() ?? _searchAgents.First().Name
                },
                TerminationStrategy = new KernelFunctionTerminationStrategy(TerminationFunction, _kernel)
                {
                    Agents = _searchAgents.Select(a => a.Agent).ToArray(),
                    MaximumIterations = 12,
                    HistoryVariableName = "lastmessage",
                    ResultParser = (result) =>
                        result.GetValue<string>()?.Contains("YES", StringComparison.OrdinalIgnoreCase) ?? false
                }
            }
        };

        string ResultParse(FunctionResult result)
        {
            var selectedAgent = _searchAgents.First(); //default to the travel planner
            var selectionFunctionResult = result.GetValue<string>();
            
            if (string.IsNullOrEmpty(selectionFunctionResult)) 
                return selectedAgent.Name;

            // Extract the agent name from the result if it contains the agent name
            foreach (var agent in _searchAgents)
            {
                if (!selectionFunctionResult.Contains(agent.Name, StringComparison.OrdinalIgnoreCase)) 
                    continue;

                selectedAgent = agent;
                break;
            }

            return selectedAgent.Name;
        }

        // Setup chat
        _chatGroup.AddChatMessage(new ChatMessageContent(AuthorRole.Developer,
            """
            You are a flight information assistant. 
            Use the search text plugin to learn about the user preferences.
            Be concise and friendly in your responses.
            """));

        _logger.LogInformation("Chat history initialized.");
    }

    public async IAsyncEnumerable<string> ConversationAsync(string userMessage)
    {
        _logger.LogTrace("Starting ConversationAsync with userMessage: {UserMessage}", userMessage);

        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
            _logger.LogInformation("Previous process canceled before starting a new one.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        _chatGroup!.IsComplete = false;
        var userChatMessage = new ChatMessageContent(AuthorRole.User, userMessage);

        _chatGroup!.AddChatMessage(userChatMessage);
        _chatMessages.Add(userChatMessage);

        _logger.LogTrace("User message added to chat history.");

        await _userPreferenceService.ExtractForUserPreferenceFactAsync(userMessage);
        _logger.LogTrace("Extracted user preference fact from user message.");

        var fullResponse = new StringBuilder();

        await foreach (var result in _chatGroup.InvokeStreamingAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Cancellation requested, stopping ConversationAsync.");
                yield break;
            }

            var content = result.Content ?? "";
            fullResponse.Append(content);

            _logger.LogTrace("Received content: {Content}", content);

            yield return content;
        }

        _chatMessages.Add(new ChatMessageContent(AuthorRole.Assistant, fullResponse.ToString()));
        _logger.LogTrace("Full assistant response added to chat history.");
    }

    public IList<ChatMessageContent> ChatMessages => _chatMessages;

    public void StopMessage()
    {
        _logger.LogInformation("Stopping the current message.");
        _cancellationTokenSource?.Cancel();
    }

    public async Task ClearChatAsync()
    {
        _logger.LogInformation("Clearing chat history.");
        StopMessage();
        await _chatGroup!.ResetAsync();
        _chatMessages.Clear();
    }

    private KernelFunction SelectionFunction =>
        AgentGroupChat.CreatePromptFunctionForStrategy(
            $"""
             Examine the provided RESPONSE, look at the goal, the current result if any, and determine which agent should respond next.

             Choose one of the following agents:
             {string.Join("\n- ", _searchAgents.Select(a => a.Name))}

             Rules:
             {string.Join("\n", _searchAgents.Select(a => $"- If {a.ActivationRule} then select \"{a.Name}\""))}
             
             - If no agent can be determined, select "TravelPlannerAgent".
             
             """ +
             """
             RESPONSE:
             {{$lastmessage}}
             """,
            safeParameterNames: ["lastmessage"]);


    private KernelFunction TerminationFunction =>
        AgentGroupChat.CreatePromptFunctionForStrategy(
            $"""
             Examine the entire conversation so far.
             
             Usually when the result contain final words such as have a safe flight, or enjoy your meal or stay, the conversation is complete, so answer "YES"

             If any key information is still missing and there is enough information from the user to continue the research, respond with the word: "NO".
             Check the user request and the system response to determine if the user's request has been fully answered.
             If one or more agent requires more information, respond with "YES" to finish the conversation, and let the user add more information.
            """ +
            """
             RESPONSE:
             {{$lastmessage}}
            """,
            safeParameterNames: ["lastmessage"]);
}
#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0110