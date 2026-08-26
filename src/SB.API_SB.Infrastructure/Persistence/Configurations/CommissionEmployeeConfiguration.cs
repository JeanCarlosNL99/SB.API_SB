using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de las propiedades propias del empleado por comision. El empleado
/// asalariado por comision hereda estas columnas al compartir la misma tabla.
/// </summary>
public sealed class CommissionEmployeeConfiguration : IEntityTypeConfiguration<CommissionEmployee>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CommissionEmployee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(employee => employee.GrossSales)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);

        builder.Property(employee => employee.CommissionRate)
            .HasPrecision(
                ColumnDefinitions.COMMISSION_RATE_PRECISION,
                ColumnDefinitions.COMMISSION_RATE_SCALE);
    }
}
