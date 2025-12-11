namespace backend.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "EventBoardPro";
    public string Audience { get; set; } = "EventBoardProClient";
    public string SecretKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
