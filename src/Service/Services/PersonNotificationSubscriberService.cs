using StackExchange.Redis;

namespace Service.Services;

// Demonstrates how another microservice would consume notifications published
// to RedisChannels.PersonUpdates.
public class PersonNotificationSubscriberService(
    IConnectionMultiplexer redis,
    ILogger<PersonNotificationSubscriberService> logger) : IHostedService
{
    private ISubscriber? _subscriber;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscriber = redis.GetSubscriber();
        await _subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannels.PersonUpdates), (channel, message) =>
        {
            logger.LogInformation("Received notification on {Channel}: {Message}", channel, message);
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscriber is not null)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(RedisChannels.PersonUpdates));
        }
    }
}
