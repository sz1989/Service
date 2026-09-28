namespace Service.Models;

public record ChatAskRequest(string Question, string ConnectionId);

public record ChatAnswerMessage(Guid RequestId, string Question, string? Answer);

public record ChatErrorMessage(Guid RequestId, string Message);

public record ChatCancelledMessage(Guid RequestId);
