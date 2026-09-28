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
    }

    public void Configure(EntityTypeBuilder<RoleRequirement> builder)
    {
        builder.ToTable("role_requirements");
        builder.HasKey(requirement => requirement.Id);
        builder.Property(requirement => requirement.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");
    }
}