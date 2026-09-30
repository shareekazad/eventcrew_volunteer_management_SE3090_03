using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCrew.Infrastructure.Persistence.Configurations;

public class ShiftPrincipalConfigurations : IEntityTypeConfiguration<Event>, IEntityTypeConfiguration<RoleRequirement>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(eventEntity => eventEntity.Id);
        builder.Property(eventEntity => eventEntity.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(eventEntity => eventEntity.Title)
            .HasColumnName("title")
            .HasColumnType("character varying(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(eventEntity => eventEntity.Status).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<RoleRequirement> builder)
    {
        builder.ToTable("role_requirements");
        builder.HasKey(requirement => requirement.Id);
        builder.Property(requirement => requirement.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(requirement => requirement.EventId)
            .HasColumnName("event_id")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(requirement => requirement.RoleName)
            .HasColumnName("role_name")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(requirement => requirement.RequiredHeadcount)
            .HasColumnName("required_headcount")
            .HasColumnType("integer")
            .IsRequired();
        builder.Property(requirement => requirement.MinExperienceLevel).HasConversion<string>();
        builder.HasOne(requirement => requirement.Event)
            .WithMany(eventEntity => eventEntity.RoleRequirements)
            .HasForeignKey(requirement => requirement.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
