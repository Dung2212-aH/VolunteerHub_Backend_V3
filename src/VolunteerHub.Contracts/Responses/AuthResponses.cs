namespace VolunteerHub.Contracts.Responses;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public AuthUserResponse User { get; set; } = new();
}

public class AuthUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
