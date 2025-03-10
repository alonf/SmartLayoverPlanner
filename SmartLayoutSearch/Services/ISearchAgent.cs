using Microsoft.SemanticKernel.Agents;

namespace SmartLayoutSearch.Services;

public interface ISearchAgent
{
    ChatCompletionAgent Agent { get; }
    string Name { get; }
    string ActivationRule { get; }
}