using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de las propiedades propias del empleado asalariado por comision.</summary>
public sealed class BaseSalariedCommissionEmployeeConfiguration
    : IEntityTypeConfiguration<BaseSalariedCommissionEmployee>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BaseSalariedCommissionEmployee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(employee => employee.BaseSalary)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);
    }
}
