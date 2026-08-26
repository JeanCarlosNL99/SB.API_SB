using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la entidad Departamento.</summary>
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    private const int NAME_MAXIMUM_LENGTH = 150;
    private const int CODE_MAXIMUM_LENGTH = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Departments");

        builder.HasKey(department => department.Id);

        builder.Property(department => department.Name)
            .IsRequired()
            .HasMaxLength(NAME_MAXIMUM_LENGTH);

        builder.Property(department => department.Code)
            .IsRequired()
            .HasMaxLength(CODE_MAXIMUM_LENGTH);

        builder.Property(department => department.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(department => department.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.HasIndex(department => department.Code)
            .IsUnique()
            .HasDatabaseName("IX_Departments_Code");
    }
}
