namespace Service.Services;

public interface IRedisPublisher
{
    Task PublishAsync(string channel, string message);
}
