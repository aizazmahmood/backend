using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(AppDbContext context, TokenService tokenService, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            OrgId = user.OrgId,
            Roles = user.Roles.Select(r => r.Role.ToString()).ToList()
        };

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        var result = await _tokenService.RefreshAccessTokenAsync(dto.RefreshToken);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        var (accessToken, user) = result.Value;

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = dto.RefreshToken, // reuse same refresh token
            Email = user.Email,
            OrgId = user.OrgId,
            Roles = user.Roles.Select(r => r.Role.ToString()).ToList()
        };

        return Ok(response);
    }
}
