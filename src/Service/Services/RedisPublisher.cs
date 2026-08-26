using StackExchange.Redis;

namespace Service.Services;

public class RedisPublisher(IConnectionMultiplexer redis) : IRedisPublisher
{
    public async Task PublishAsync(string channel, string message)
    {
        var subscriber = redis.GetSubscriber();
        await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
    }
}
