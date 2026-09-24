namespace Service.BAL.Rag;

public interface IRagService
{
    Task<string> Ask(string question);
}
