namespace Service.Authentication;

public class AuthOptions
{
    public List<AuthUser> Users { get; set; } = [];
}

public class AuthUser
{
    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public string[] Roles { get; set; } = [];
}
