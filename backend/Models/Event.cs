using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string OrgId { get; set; } = default!;

    [Required]
    public int CreatorId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Category { get; set; } = default!;

    [Required]
    public EventStatus Status { get; set; } = EventStatus.Pending;

    public bool IsFeatured { get; set; }

    // Comma-separated tags internally
    public string Tags { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
