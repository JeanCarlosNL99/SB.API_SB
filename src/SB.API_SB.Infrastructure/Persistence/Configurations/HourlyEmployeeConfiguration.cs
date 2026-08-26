using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de las propiedades propias del empleado por horas.</summary>
public sealed class HourlyEmployeeConfiguration : IEntityTypeConfiguration<HourlyEmployee>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HourlyEmployee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(employee => employee.HourlyWage)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);

        builder.Property(employee => employee.HoursWorked)
            .HasPrecision(ColumnDefinitions.HOURS_PRECISION, ColumnDefinitions.HOURS_SCALE);

        // Las horas ordinarias y extras se derivan de las horas trabajadas.
        builder.Ignore(employee => employee.RegularHours);
        builder.Ignore(employee => employee.OvertimeHours);
    }
}
