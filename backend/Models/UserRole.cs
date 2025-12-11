namespace backend.Models;

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public Role Role { get; set; }
}
