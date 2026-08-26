using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de las propiedades propias del empleado asalariado.</summary>
public sealed class SalariedEmployeeConfiguration : IEntityTypeConfiguration<SalariedEmployee>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SalariedEmployee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(employee => employee.WeeklySalary)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);
    }
}
