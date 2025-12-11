using System.Text;
using backend.Auth;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly RbacService _rbac;

    public EventsController(AppDbContext context, CurrentUserService currentUser, RbacService rbac)
    {
        _context = context;
        _currentUser = currentUser;
        _rbac = rbac;
    }

    [HttpGet]
    public async Task<ActionResult<EventListResponseDto>> GetEvents(
        [FromQuery] string? scope,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery(Name = "q")] string? search,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var roles = _currentUser.Roles;
        var userId = _currentUser.UserId;
        var orgId = _currentUser.OrgId;

        var query = _context.Events.AsQueryable();

        var effectiveScope = scope;
        if (string.IsNullOrEmpty(effectiveScope))
        {
            if (roles.Contains(Role.Admin) || roles.Contains(Role.Moderator))
                effectiveScope = "org";
            else
                effectiveScope = "mine";
        }

        effectiveScope = effectiveScope.ToLowerInvariant();

        switch (effectiveScope)
        {
            case "mine":
                query = query.Where(e => e.CreatorId == userId);
                break;

            case "all":
                if (roles.Contains(Role.Admin))
                {
                    // no org filter
                }
                else
                {
                    query = query.Where(e => e.OrgId == orgId);
                }
                break;

            case "org":
            default:
                query = query.Where(e => e.OrgId == orgId);
                break;
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<EventStatus>(status, true, out var st))
        {
            query = query.Where(e => e.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Title.Contains(search));
        }

        // Cursor: UpdatedAt DESC, Id DESC
        if (!string.IsNullOrEmpty(cursor))
        {
            if (TryDecodeCursor(cursor, out var cursorTime, out var cursorId))
            {
                query = query.Where(e =>
                    e.UpdatedAt < cursorTime ||
                    (e.UpdatedAt == cursorTime && e.Id < cursorId));
            }
        }

        query = query
            .OrderByDescending(e => e.UpdatedAt)
            .ThenByDescending(e => e.Id);

        var items = await query.Take(limit + 1).ToListAsync();

        string? nextCursor = null;
        if (items.Count > limit)
        {
            var last = items[^1];
            nextCursor = EncodeCursor(last.UpdatedAt, last.Id);
            items.RemoveAt(items.Count - 1);
        }

        var dto = new EventListResponseDto
        {
            Items = items.Select(ToDto).ToList(),
            NextCursor = nextCursor
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventDto dto)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        var now = DateTime.UtcNow;

        var ev = new Event
        {
            OrgId = _currentUser.OrgId,
            CreatorId = _currentUser.UserId,
            Title = dto.Title,
            Category = dto.Category,
            Tags = dto.Tags != null ? string.Join(",", dto.Tags) : string.Empty,
            Status = EventStatus.Pending,
            IsFeatured = false,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return Ok(ToDto(ev));
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<EventDto>> Update(int id, [FromBody] UpdateEventDto dto)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null)
            return NotFound();

        if (!_rbac.CanManageEvent(_currentUser.UserId, _currentUser.OrgId, _currentUser.Roles, ev))
            return Forbid();

        if (!string.IsNullOrWhiteSpace(dto.Title))
            ev.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Category))
            ev.Category = dto.Category;

        if (dto.Tags != null)
            ev.Tags = string.Join(",", dto.Tags);

        if (dto.IsFeatured.HasValue)
            ev.IsFeatured = dto.IsFeatured.Value;

        if (!string.IsNullOrWhiteSpace(dto.Status) &&
            Enum.TryParse<EventStatus>(dto.Status, true, out var st))
        {
            ev.Status = st;
        }

        if (dto.StartDate.HasValue)
            ev.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            ev.EndDate = dto.EndDate.Value;

        ev.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToDto(ev));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null)
            return NotFound();

        if (!_rbac.CanManageEvent(_currentUser.UserId, _currentUser.OrgId, _currentUser.Roles, ev))
            return Forbid();

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("bulk")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequestDto dto)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized();

        if (dto.Ids == null || dto.Ids.Count == 0)
            return BadRequest(new { message = "No ids provided" });

        var events = await _context.Events
            .Where(e => dto.Ids.Contains(e.Id))
            .ToListAsync();

        if (events.Count == 0)
            return NotFound();

        var roles = _currentUser.Roles;
        var userId = _currentUser.UserId;
        var orgId = _currentUser.OrgId;

        var now = DateTime.UtcNow;

        foreach (var ev in events)
        {
            if (!_rbac.CanManageEvent(userId, orgId, roles, ev))
            {
                continue; // skip events user cannot manage
            }

            switch (dto.Action.ToLowerInvariant())
            {
                case "approve":
                    ev.Status = EventStatus.Approved;
                    break;
                case "reject":
                    ev.Status = EventStatus.Rejected;
                    break;
                case "feature":
                    ev.IsFeatured = true;
                    break;
                case "unfeature":
                    ev.IsFeatured = false;
                    break;
                default:
                    return BadRequest(new { message = "Invalid action" });
            }

            ev.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Helpers

    private static EventDto ToDto(Event e)
    {
        return new EventDto
        {
            Id = e.Id,
            OrgId = e.OrgId,
            CreatorId = e.CreatorId,
            Title = e.Title,
            Category = e.Category,
            Status = e.Status.ToString(),
            IsFeatured = e.IsFeatured,
            Tags = string.IsNullOrWhiteSpace(e.Tags)
                ? Array.Empty<string>()
                : e.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    private static string EncodeCursor(DateTime updatedAt, int id)
    {
        var data = $"{updatedAt.Ticks}:{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    private static bool TryDecodeCursor(string cursor, out DateTime updatedAt, out int id)
    {
        updatedAt = default;
        id = default;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var s = Encoding.UTF8.GetString(bytes);
            var parts = s.Split(':', 2);
            if (parts.Length != 2)
                return false;

            if (!long.TryParse(parts[0], out var ticks))
                return false;
            if (!int.TryParse(parts[1], out var parsedId))
                return false;

            updatedAt = new DateTime(ticks, DateTimeKind.Utc);
            id = parsedId;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
