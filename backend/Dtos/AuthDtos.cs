using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = default!;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public List<string> Roles { get; set; } = new();
}
