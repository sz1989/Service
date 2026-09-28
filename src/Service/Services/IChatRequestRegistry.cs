namespace Service.Services;

public interface IChatRequestRegistry
{
    void Register(Guid requestId);

    bool TryGetToken(Guid requestId, out CancellationToken token);

    bool TryCancel(Guid requestId);

    void Complete(Guid requestId);
}
