namespace Service.BAL.Chat;

public interface IChatService
{
    Task<string?> AskAsync(string question, CancellationToken cancellationToken = default);
}
