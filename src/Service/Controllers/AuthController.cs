using Service.BAL.Auth;

namespace Service.Controllers;

[AllowAnonymous]
[ApiController, Route("[controller]")]
public class AuthController(
    ILogger<AuthController> logger,
    IAuthService authService) : ControllerBase
{
    public record LoginRequest(string Username, string Password);

    public record LoginResponse(string Token);

    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        var token = authService.Authenticate(request.Username, request.Password);
        if (token is null)
        {
            logger.LogWarning("Failed login attempt for {Username}", request.Username);
            return Unauthorized();
        }

        logger.LogInformation("User {Username} logged in", request.Username);

        return Ok(new LoginResponse(token));
    }
}
