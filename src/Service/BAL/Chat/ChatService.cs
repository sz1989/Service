using Microsoft.Extensions.AI;

namespace Service.BAL.Chat;

public class ChatService(IChatClient chatClient) : IChatService
{
    public async Task<string?> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var response = await chatClient.GetResponseAsync(question, cancellationToken: cancellationToken);

        return response.Text;
    }
}
