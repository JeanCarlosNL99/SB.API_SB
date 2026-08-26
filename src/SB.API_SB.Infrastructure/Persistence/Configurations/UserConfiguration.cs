using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la entidad Usuario.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private const int USER_NAME_MAXIMUM_LENGTH = 60;
    private const int EMAIL_MAXIMUM_LENGTH = 150;
    private const int FULL_NAME_MAXIMUM_LENGTH = 100;
    private const int PASSWORD_HASH_MAXIMUM_LENGTH = 256;
    private const int PASSWORD_SALT_MAXIMUM_LENGTH = 128;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName)
            .IsRequired()
            .HasMaxLength(USER_NAME_MAXIMUM_LENGTH);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(EMAIL_MAXIMUM_LENGTH);

        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(FULL_NAME_MAXIMUM_LENGTH);

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(PASSWORD_HASH_MAXIMUM_LENGTH);

        builder.Property(user => user.PasswordSalt)
            .IsRequired()
            .HasMaxLength(PASSWORD_SALT_MAXIMUM_LENGTH);

        builder.Property(user => user.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(user => user.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.HasIndex(user => user.UserName)
            .IsUnique()
            .HasDatabaseName("IX_Users_UserName");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");
    }
}
