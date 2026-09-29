using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCrew.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(user => user.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        builder.Property(user => user.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        builder.Property(user => user.IsActive).HasColumnName("is_active").IsRequired();
    }
}

public sealed class VolunteerProfileConfiguration : IEntityTypeConfiguration<VolunteerProfile>
{
    public void Configure(EntityTypeBuilder<VolunteerProfile> builder)
    {
        builder.ToTable("volunteer_profiles");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(profile => profile.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.HasOne(profile => profile.User)
            .WithOne()
            .HasForeignKey<VolunteerProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("applications");
        builder.HasKey(application => application.Id);
        builder.Property(application => application.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(application => application.EventId).HasColumnName("event_id").HasColumnType("uuid");
        builder.Property(application => application.VolunteerProfileId).HasColumnName("volunteer_id").HasColumnType("uuid");
        builder.Property(application => application.RoleRequirementId).HasColumnName("role_requirement_id").HasColumnType("uuid");
        builder.Property(application => application.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.HasOne<Event>().WithMany().HasForeignKey(application => application.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<VolunteerProfile>().WithMany().HasForeignKey(application => application.VolunteerProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RoleRequirement>().WithMany().HasForeignKey(application => application.RoleRequirementId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("shift_assignments", table =>
            table.HasCheckConstraint("shift_assignments_status_check", "status IN ('Proposed_By_AI', 'Confirmed', 'Declined', 'Completed', 'Cancelled')"));
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(assignment => assignment.ShiftId).HasColumnName("shift_id").HasColumnType("uuid").IsRequired();
        builder.Property(assignment => assignment.VolunteerId).HasColumnName("volunteer_id").HasColumnType("uuid").IsRequired();
        builder.Property(assignment => assignment.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Proposed_By_AI").IsRequired();
        builder.Property(assignment => assignment.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(assignment => assignment.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(assignment => assignment.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(assignment => new { assignment.ShiftId, assignment.VolunteerId }).IsUnique().HasDatabaseName("uq_shift_volunteer");
        builder.HasOne(assignment => assignment.Shift)
            .WithMany(shift => shift.Assignments)
            .HasForeignKey(assignment => assignment.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(assignment => assignment.Volunteer)
            .WithMany(profile => profile.Assignments)
            .HasForeignKey(assignment => assignment.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}