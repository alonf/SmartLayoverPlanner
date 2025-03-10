using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;

namespace SmartLayoutSearch.Services;

public interface IChatService
{
    Task InitializeAsync();
    IAsyncEnumerable<string> ConversationAsync(string userMessage);
    IList<ChatMessageContent> ChatMessages { get; }
    void StopMessage();
    Task ClearChatAsync();
}
