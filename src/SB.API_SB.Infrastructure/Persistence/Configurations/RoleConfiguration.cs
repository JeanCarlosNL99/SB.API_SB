using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la entidad Rol.</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private const int NAME_MAXIMUM_LENGTH = 60;
    private const int DESCRIPTION_MAXIMUM_LENGTH = 250;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .IsRequired()
            .HasMaxLength(NAME_MAXIMUM_LENGTH);

        builder.Property(role => role.Description)
            .IsRequired()
            .HasMaxLength(DESCRIPTION_MAXIMUM_LENGTH);

        builder.Property(role => role.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(role => role.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.HasIndex(role => role.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");
    }
}
