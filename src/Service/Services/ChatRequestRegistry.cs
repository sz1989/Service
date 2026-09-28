using System.Collections.Concurrent;

namespace Service.Services;

public class ChatRequestRegistry : IChatRequestRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pending = new();

    public void Register(Guid requestId) => _pending[requestId] = new CancellationTokenSource();

    public bool TryGetToken(Guid requestId, out CancellationToken token)
    {
        if (_pending.TryGetValue(requestId, out var cts))
        {
            token = cts.Token;
            return true;
        }

        token = CancellationToken.None;
        return false;
    }

    public bool TryCancel(Guid requestId)
    {
        if (_pending.TryGetValue(requestId, out var cts))
        {
            cts.Cancel();
            return true;
        }

        return false;
    }

    public void Complete(Guid requestId)
    {
        if (_pending.TryRemove(requestId, out var cts))
        {
            cts.Dispose();
        }
    }
}
