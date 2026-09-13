namespace Service.BAL.Auth;

public interface IAuthService
{
    /// <summary>Validates credentials and returns a signed JWT, or null if they don't match a known user.</summary>
    string? Authenticate(string username, string password);
}
