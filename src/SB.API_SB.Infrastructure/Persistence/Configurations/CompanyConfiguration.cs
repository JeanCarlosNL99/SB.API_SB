using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de la entidad Compania.</summary>
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    private const int NAME_MAXIMUM_LENGTH = 200;
    private const int TAX_IDENTIFICATION_MAXIMUM_LENGTH = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .IsRequired()
            .HasMaxLength(NAME_MAXIMUM_LENGTH);

        builder.Property(company => company.TaxIdentificationNumber)
            .IsRequired()
            .HasMaxLength(TAX_IDENTIFICATION_MAXIMUM_LENGTH);

        builder.Property(company => company.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(company => company.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.HasIndex(company => company.TaxIdentificationNumber)
            .IsUnique()
            .HasDatabaseName("IX_Companies_TaxIdentificationNumber");
    }
}
