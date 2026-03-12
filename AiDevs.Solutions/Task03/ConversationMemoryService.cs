using System.Collections.Concurrent;
using AiDevs.Infrastructure.Models;

namespace AiDevs.Solutions.Task03;

/// <summary>
/// Service for managing conversation history per session
/// </summary>
public interface IConversationMemoryService
{
    void AddMessage(string sessionId, OpenRouterMessage message);
    List<OpenRouterMessage> GetMessages(string sessionId);
    void ClearSession(string sessionId);
}

public class ConversationMemoryService : IConversationMemoryService
{
    private readonly ConcurrentDictionary<string, List<OpenRouterMessage>> _sessions = new();

    public void AddMessage(string sessionId, OpenRouterMessage message)
    {
        var messages = _sessions.GetOrAdd(sessionId, _ => new List<OpenRouterMessage>());
        lock (messages)
        {
            messages.Add(message);
        }
    }

    public List<OpenRouterMessage> GetMessages(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var messages))
        {
            lock (messages)
            {
                return new List<OpenRouterMessage>(messages);
            }
        }
        return new List<OpenRouterMessage>();
    }

    public void ClearSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}
