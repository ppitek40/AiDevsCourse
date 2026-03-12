using AiDevs.Core.Models;

namespace AiDevs.Infrastructure.Services;

public interface IProxyEventAggregator
{
    void Publish(StreamUpdate proxyEvent);
    IAsyncEnumerable<StreamUpdate> Subscribe(string sessionId);
    void CompleteSession(string sessionId);
}

public class ProxyEventAggregator : IProxyEventAggregator
{
    private EventChannel _channel = new();

    public void Publish(StreamUpdate proxyEvent)
    {
            _channel.Writer.TryWrite(proxyEvent);
    }

    public async IAsyncEnumerable<StreamUpdate> Subscribe(string sessionId)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync())
        {
            yield return evt;
        }
    }

    public void CompleteSession(string sessionId)
    {
        _channel.Writer.Complete();
    }

    private class EventChannel
    {
        private readonly System.Threading.Channels.Channel<StreamUpdate> _channel;

        public EventChannel()
        {
            _channel = System.Threading.Channels.Channel.CreateUnbounded<StreamUpdate>();
        }

        public System.Threading.Channels.ChannelWriter<StreamUpdate> Writer => _channel.Writer;
        public System.Threading.Channels.ChannelReader<StreamUpdate> Reader => _channel.Reader;
    }
}
