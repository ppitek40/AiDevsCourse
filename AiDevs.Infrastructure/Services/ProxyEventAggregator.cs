using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AiDevs.Core.Models;

namespace AiDevs.Infrastructure.Services;

public interface IProxyEventAggregator
{
    void Init();
    void Publish(StreamUpdate proxyEvent);

    IAsyncEnumerable<StreamUpdate> Subscribe(
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);

    void CompleteSession(string sessionId);
}

public class ProxyEventAggregator : IProxyEventAggregator
{
    private EventChannel _channel = new();

    public void Init()
    {
        _channel = new EventChannel();
    }

    public void Publish(StreamUpdate proxyEvent)
    {
        _channel.Writer.TryWrite(proxyEvent);
    }

    public async IAsyncEnumerable<StreamUpdate> Subscribe(
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(cancellationToken))
            yield return evt;
    }

    public void CompleteSession(string sessionId)
    {
        _channel.Writer.Complete();
    }

    private class EventChannel
    {
        private readonly Channel<StreamUpdate> _channel;

        public EventChannel()
        {
            _channel = Channel.CreateUnbounded<StreamUpdate>();
        }

        public ChannelWriter<StreamUpdate> Writer => _channel.Writer;
        public ChannelReader<StreamUpdate> Reader => _channel.Reader;
    }
}