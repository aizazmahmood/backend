using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context, IPasswordHasher<User> hasher)
    {
        if (context.Users.Any())
        {
            return; // already seeded
        }

        var now = DateTime.UtcNow;

        // Users
        var adminOrgA = new User
        {
            Email = "admin@orga.com",
            OrgId = "orgA",
            CreatedAt = now
        };
        adminOrgA.PasswordHash = hasher.HashPassword(adminOrgA, "Password1!");

        var modOrgA = new User
        {
            Email = "mod@orga.com",
            OrgId = "orgA",
            CreatedAt = now
        };
        modOrgA.PasswordHash = hasher.HashPassword(modOrgA, "Password1!");

        var user1OrgA = new User
        {
            Email = "user1@orga.com",
            OrgId = "orgA",
            CreatedAt = now
        };
        user1OrgA.PasswordHash = hasher.HashPassword(user1OrgA, "Password1!");

        var user2OrgA = new User
        {
            Email = "user2@orga.com",
            OrgId = "orgA",
            CreatedAt = now
        };
        user2OrgA.PasswordHash = hasher.HashPassword(user2OrgA, "Password1!");


        var user3OrgB = new User
        {
            Email = "user3@orgb.com",
            OrgId = "orgB",
            CreatedAt = now
        };
        user3OrgB.PasswordHash = hasher.HashPassword(user3OrgB, "Password1!");

        var modOrgB = new User
        {
            Email = "mod@orgb.com",
            OrgId = "orgB",
            CreatedAt = now
        };
        modOrgB.PasswordHash = hasher.HashPassword(modOrgB, "Password1!");

        adminOrgA.Roles.Add(new UserRole { User = adminOrgA, Role = Role.Admin });
        modOrgA.Roles.Add(new UserRole { User = modOrgA, Role = Role.Moderator });
        modOrgB.Roles.Add(new UserRole { User = modOrgB, Role = Role.Moderator });

        user1OrgA.Roles.Add(new UserRole { User = user1OrgA, Role = Role.User });
        user2OrgA.Roles.Add(new UserRole { User = user2OrgA, Role = Role.User });
        user3OrgB.Roles.Add(new UserRole { User = user3OrgB, Role = Role.User });

        context.Users.AddRange(adminOrgA, modOrgA, user1OrgA, user2OrgA, user3OrgB, modOrgB);
        context.SaveChanges();

        // Refresh users from DB to get IDs (optional but safe)
        var users = context.Users.ToList();

        var categories = new[] { "Conference", "Meetup", "Workshop", "Webinar" };
        var statuses = new[] { EventStatus.Pending, EventStatus.Approved, EventStatus.Rejected };

        var rnd = new Random();

        var events = new List<Event>();
        for (int i = 1; i <= 40; i++)
        {
            var user = users[rnd.Next(users.Count)];
            var start = now.AddDays(rnd.Next(-30, 30));
            var end = start.AddDays(1);

            var ev = new Event
            {
                OrgId = user.OrgId,
                CreatorId = user.Id,
                Title = $"Seed Event {i} for {user.OrgId}",
                Category = categories[rnd.Next(categories.Length)],
                Status = statuses[rnd.Next(statuses.Length)],
                IsFeatured = rnd.NextDouble() < 0.25,
                Tags = "sample,seed",
                StartDate = start,
                EndDate = end,
                CreatedAt = start.AddDays(-1),
                UpdatedAt = start
            };
            events.Add(ev);
        }

        context.Events.AddRange(events);
        context.SaveChanges();
    }
}
