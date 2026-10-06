using EventCrew.Domain.Entities;
using EventCrew.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch
        {
            // Database might already exist or migrations managed externally
        }

        if (await context.Users.AnyAsync())
        {
            return; // DB already seeded
        }

        // 1. Seed Lead Organizer User (John)
        var johnUser = new User
        {
            Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            FullName = "John",
            Email = "john@eventcrew.com",
            Role = "Organizer",
            PasswordHash = "hashed_default_password_dev_123",
            PhoneNumber = "0771234567",
            IsActive = true
        };
        context.Users.Add(johnUser);

        // 2. Seed Sample Event (TechFest 2026) organized by John
        var eventId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var techFestEvent = new Event
        {
            Id = eventId,
            OrganizerId = johnUser.Id,
            Title = "TechFest 2026",
            Description = "Annual premier technology conference & innovation showcase.",
            Category = "Technology",
            StartDate = DateTimeOffset.UtcNow.AddDays(30),
            EndDate = DateTimeOffset.UtcNow.AddDays(32),
            Status = EventStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Events.Add(techFestEvent);

        // 3. Seed 5 Standard Skills
        var skills = new List<Skill>
        {
            new Skill { Id = Guid.NewGuid(), Name = "First Aid", Category = "Medical", Description = "Emergency first aid and medical response." },
            new Skill { Id = Guid.NewGuid(), Name = "Crowd Safety", Category = "Operations", Description = "Crowd management and venue safety protocols." },
            new Skill { Id = Guid.NewGuid(), Name = "Audio/Visual", Category = "Technical", Description = "AV equipment operation and stage tech support." },
            new Skill { Id = Guid.NewGuid(), Name = "Logistics", Category = "Operations", Description = "Equipment setup, inventory, and venue logistics." },
            new Skill { Id = Guid.NewGuid(), Name = "IT Support", Category = "Technical", Description = "Network, registration, and badge printing tech support." }
        };
        context.Skills.AddRange(skills);

        var firstAidSkill = skills[0];
        var crowdSafetySkill = skills[1];
        var avSkill = skills[2];
        var logisticsSkill = skills[3];
        var itSupportSkill = skills[4];

        // 4. Seed 4 Team Members as Volunteer Users + VolunteerProfiles + VolunteerSkills
        var teamMembers = new[]
        {
            new
            {
                Name = "Samadhi",
                Email = "samadhi@eventcrew.com",
                Rating = 5.00m,
                Bio = "Certified emergency response and crowd management lead.",
                Skills = new[] { (Skill: firstAidSkill, Level: "Advanced"), (Skill: crowdSafetySkill, Level: "Advanced") },
                AppStatus = "UnderReview",
                AppNotes = "Available for emergency response and medical station management."
            },
            new
            {
                Name = "Thisunii",
                Email = "thisunii@eventcrew.com",
                Rating = 4.80m,
                Bio = "Experienced AV technician and stage coordinator.",
                Skills = new[] { (Skill: avSkill, Level: "Advanced"), (Skill: logisticsSkill, Level: "Intermediate") },
                AppStatus = "Submitted",
                AppNotes = "Available for stage setup and sound desk operations."
            },
            new
            {
                Name = "Sithuli",
                Email = "sithuli@eventcrew.com",
                Rating = 4.90m,
                Bio = "Tech support and registration logistics specialist.",
                Skills = new[] { (Skill: itSupportSkill, Level: "Advanced"), (Skill: crowdSafetySkill, Level: "Intermediate") },
                AppStatus = "Shortlisted",
                AppNotes = "Experienced with attendee check-in systems and badge printers."
            },
            new
            {
                Name = "Shareeka",
                Email = "shareeka@eventcrew.com",
                Rating = 4.70m,
                Bio = "Logistics, operations, and venue setup crew.",
                Skills = new[] { (Skill: logisticsSkill, Level: "Advanced"), (Skill: firstAidSkill, Level: "Intermediate") },
                AppStatus = "Submitted",
                AppNotes = "Ready to assist with heavy equipment logistics and venue setup."
            }
        };

        var now = DateTime.UtcNow;

        foreach (var (member, idx) in teamMembers.Select((m, i) => (m, i)))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = member.Name,
                Email = member.Email,
                Role = "Volunteer",
                PasswordHash = "hashed_default_password_dev_123",
                PhoneNumber = "0771234567",
                IsActive = true
            };
            context.Users.Add(user);

            var profile = new VolunteerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EmergencyContact = $"+1 (555) 100-000{idx + 1}",
                Bio = member.Bio,
                MaxHoursPerWeek = 20,
                RatingScore = member.Rating,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.VolunteerProfiles.Add(profile);

            foreach (var s in member.Skills)
            {
                context.VolunteerSkills.Add(new VolunteerSkill
                {
                    VolunteerId = profile.Id,
                    SkillId = s.Skill.Id,
                    ProficiencyLevel = s.Level
                });
            }

            // 5. Seed Applications for TechFest 2026
            var app = new Application
            {
                Id = Guid.NewGuid(),
                EventId = techFestEvent.Id,
                VolunteerId = profile.Id,
                Status = member.AppStatus,
                Notes = member.AppNotes,
                AppliedAt = now.AddHours(-12 - (idx * 6)),
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Applications.Add(app);
        }

        await context.SaveChangesAsync();
    }
}
