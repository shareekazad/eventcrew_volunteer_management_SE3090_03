using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCrew.Infrastructure.Persistence.Configurations;

public class ShiftSwapRequestConfiguration : IEntityTypeConfiguration<ShiftSwapRequest>
{
    public void Configure(EntityTypeBuilder<ShiftSwapRequest> builder)
    {
        builder.ToTable("shift_swap_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.RequesterAssignmentId).HasColumnName("requester_assignment_id").IsRequired();
        builder.Property(r => r.TargetVolunteerId).HasColumnName("target_volunteer_id").IsRequired();
        builder.Property(r => r.TargetShiftId).HasColumnName("target_shift_id").IsRequired();
        builder.Property(r => r.Reason).HasColumnName("reason");
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(25).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne(r => r.RequesterAssignment)
            .WithMany()
            .HasForeignKey(r => r.RequesterAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.TargetVolunteer)
            .WithMany()
            .HasForeignKey(r => r.TargetVolunteerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TargetShift)
            .WithMany()
            .HasForeignKey(r => r.TargetShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
