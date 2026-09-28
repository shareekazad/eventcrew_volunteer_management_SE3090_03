using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCrew.Infrastructure.Persistence.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("shifts", table =>
        {
            table.HasCheckConstraint("shifts_capacity_check", "capacity > 0");
            table.HasCheckConstraint("shifts_status_check", "status IN ('Draft', 'Scheduled', 'InProgress', 'Completed', 'Cancelled')");
            table.HasCheckConstraint("chk_shift_times", "start_time < end_time");
        });

        builder.HasKey(shift => shift.Id);

        builder.Property(shift => shift.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(shift => shift.EventId)
            .HasColumnName("event_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(shift => shift.RoleRequirementId)
            .HasColumnName("role_requirement_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(shift => shift.Title)
            .HasColumnName("title")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(shift => shift.StartTime)
            .HasColumnName("start_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(shift => shift.EndTime)
            .HasColumnName("end_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(shift => shift.Capacity)
            .HasColumnName("capacity")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(shift => shift.Status)
            .HasColumnName("status")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .HasDefaultValue("Scheduled")
            .IsRequired();

        builder.Property(shift => shift.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(shift => shift.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(shift => shift.Event)
            .WithMany(eventEntity => eventEntity.Shifts)
            .HasForeignKey(shift => shift.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(shift => shift.RoleRequirement)
            .WithMany(requirement => requirement.Shifts)
            .HasForeignKey(shift => shift.RoleRequirementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}