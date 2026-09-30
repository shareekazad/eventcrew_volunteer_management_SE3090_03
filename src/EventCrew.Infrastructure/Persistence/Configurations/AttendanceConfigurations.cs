using EventCrew.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventCrew.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", table =>
            table.HasCheckConstraint("attendance_records_status_check", "status IN ('Pending', 'CheckedIn', 'CheckedOut', 'Absent', 'Excused')"));
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(record => record.ShiftAssignmentId).HasColumnName("shift_assignment_id").HasColumnType("uuid").IsRequired();
        builder.Property(record => record.CheckInTime).HasColumnName("check_in_time").HasColumnType("timestamp with time zone");
        builder.Property(record => record.CheckOutTime).HasColumnName("check_out_time").HasColumnType("timestamp with time zone");
        builder.Property(record => record.CheckInLatitude).HasColumnName("check_in_latitude").HasColumnType("numeric(9,6)");
        builder.Property(record => record.CheckInLongitude).HasColumnName("check_in_longitude").HasColumnType("numeric(9,6)");
        builder.Property(record => record.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
        builder.Property(record => record.VerifiedHours).HasColumnName("verified_hours").HasColumnType("numeric(5,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(record => record.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.HasIndex(record => record.ShiftAssignmentId).IsUnique().HasDatabaseName("attendance_records_shift_assignment_id_key");
        builder.HasOne(record => record.ShiftAssignment).WithOne(assignment => assignment.AttendanceRecord)
            .HasForeignKey<AttendanceRecord>(record => record.ShiftAssignmentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class QrCodeTokenConfiguration : IEntityTypeConfiguration<QrCodeToken>
{
    public void Configure(EntityTypeBuilder<QrCodeToken> builder)
    {
        builder.ToTable("qr_code_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(token => token.ShiftId).HasColumnName("shift_id").HasColumnType("uuid").IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(token => token.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasOne(token => token.Shift).WithMany(shift => shift.QrCodeTokens).HasForeignKey(token => token.ShiftId).OnDelete(DeleteBehavior.Cascade);
    }
}
