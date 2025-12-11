using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class User
{
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string PasswordHash { get; set; } = default!;

    [Required]
    public string OrgId { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}
