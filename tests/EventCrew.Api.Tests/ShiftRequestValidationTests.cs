using System.ComponentModel.DataAnnotations;
using EventCrew.Api.Dtos;

namespace EventCrew.Api.Tests;

public class ShiftRequestValidationTests
{
    [Fact]
    public void CreateRequestRejectsNonPositiveCapacity()
    {
        var request = ValidCreateRequest() with { Capacity = 0 };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateShiftRequest.Capacity)));
    }

    [Fact]
    public void CreateRequestRejectsInvalidTimeRange()
    {
        var request = ValidCreateRequest() with
        {
            StartTime = DateTimeOffset.Parse("2026-10-01T12:00:00Z"),
            EndTime = DateTimeOffset.Parse("2026-10-01T11:00:00Z")
        };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateShiftRequest.StartTime)));
    }

    [Fact]
    public void CreateRequestRejectsMissingForeignKeyIdentifiers()
    {
        var request = ValidCreateRequest() with { EventId = Guid.Empty };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateShiftRequest.EventId)));
    }

    [Fact]
    public void CreateRequestRejectsMissingRoleRequirementIdentifier()
    {
        var request = ValidCreateRequest() with { RoleRequirementId = Guid.Empty };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateShiftRequest.RoleRequirementId)));
    }

    [Fact]
    public void CreateRequestRejectsBlankTitle()
    {
        var request = ValidCreateRequest() with { Title = " " };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateShiftRequest.Title)));
    }

    [Fact]
    public void RoleRequirementMustBelongToSelectedEvent()
    {
        var eventId = Guid.NewGuid();
        var requirement = new EventCrew.Domain.Entities.RoleRequirement { EventId = eventId };

        Assert.True(requirement.BelongsToEvent(eventId));
        Assert.False(requirement.BelongsToEvent(Guid.NewGuid()));
    }

    private static CreateShiftRequest ValidCreateRequest() => new()
    {
        Title = "Check-in support",
        EventId = Guid.NewGuid(),
        RoleRequirementId = Guid.NewGuid(),
        StartTime = DateTimeOffset.Parse("2026-10-01T10:00:00Z"),
        EndTime = DateTimeOffset.Parse("2026-10-01T11:00:00Z"),
        Capacity = 1
    };

    private static List<ValidationResult> Validate(object instance)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), errors, validateAllProperties: true);
        return errors;
    }
}