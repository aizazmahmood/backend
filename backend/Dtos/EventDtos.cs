using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class EventDto
{
    public int Id { get; set; }
    public string OrgId { get; set; } = default!;
    public int CreatorId { get; set; }
    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Status { get; set; } = default!;
    public bool IsFeatured { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEventDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Category { get; set; } = default!;

    public string[]? Tags { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class UpdateEventDto
{
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string[]? Tags { get; set; }
    public bool? IsFeatured { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class BulkUpdateRequestDto
{
    [Required]
    public string Action { get; set; } = default!; // approve | reject | feature | unfeature

    [Required]
    public List<int> Ids { get; set; } = new();
}

public class EventListResponseDto
{
    public List<EventDto> Items { get; set; } = new();
    public string? NextCursor { get; set; }
}
