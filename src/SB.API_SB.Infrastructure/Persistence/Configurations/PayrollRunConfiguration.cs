using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SB.API_SB.Domain.Entities;

namespace SB.API_SB.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de la ejecucion de nomina.
/// </summary>
/// <remarks>
/// El indice unico filtrado sobre (GovernmentEntityId, Year, WeekNumber) es la
/// pieza clave: hace que la base de datos rechace una segunda nomina vigente para
/// la misma semana. La comprobacion en el servicio da un mensaje claro al
/// usuario, pero es este indice el que cierra la ventana de una condicion de
/// carrera entre dos peticiones simultaneas.
/// </remarks>
public sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    private const int CANCELLATION_REASON_MAXIMUM_LENGTH = 500;
    private const int GOVERNMENT_ENTITY_NAME_MAXIMUM_LENGTH = 250;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PayrollRuns");

        builder.HasKey(payrollRun => payrollRun.Id);

        builder.Property(payrollRun => payrollRun.Year).IsRequired();
        builder.Property(payrollRun => payrollRun.WeekNumber).IsRequired();
        builder.Property(payrollRun => payrollRun.WeekStartDate).IsRequired();
        builder.Property(payrollRun => payrollRun.WeekEndDate).IsRequired();

        builder.Property(payrollRun => payrollRun.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(payrollRun => payrollRun.TotalAmount)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);

        builder.Property(payrollRun => payrollRun.CancellationReason)
            .HasMaxLength(CANCELLATION_REASON_MAXIMUM_LENGTH);

        builder.Property(payrollRun => payrollRun.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(payrollRun => payrollRun.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        // El nombre de la entidad gubernamental se almacena en el documento porque
        // el listado oficial vive en el archivo de texto plano: no hay clave
        // foranea posible hacia el, y por tanto ninguna consulta que lo una con el
        // historial. Guardar el nombre es lo que hace la nomina legible por si
        // sola, tal como ya ocurre con los datos del empleado en cada linea.
        builder.Property(payrollRun => payrollRun.GovernmentEntityName)
            .IsRequired()
            .HasMaxLength(GOVERNMENT_ENTITY_NAME_MAXIMUM_LENGTH);

        // Una semana solo puede tener una nomina vigente. El filtro deja fuera las
        // anuladas, de modo que anular libera la semana para recalcularla.
        builder.HasIndex(payrollRun => new
            {
                payrollRun.GovernmentEntityId,
                payrollRun.Year,
                payrollRun.WeekNumber
            })
            .IsUnique()
            // Se usan comillas dobles como delimitador de identificador porque las
            // admiten tanto SQLite como SQL Server, manteniendo el indice portable.
            .HasFilter("\"Status\" = 1")
            .HasDatabaseName("IX_PayrollRuns_Entidad_Ano_Semana_Vigente");

        // Indice que respalda el listado del historial, ordenado por periodo.
        builder.HasIndex(payrollRun => new
            {
                payrollRun.GovernmentEntityId,
                payrollRun.Year,
                payrollRun.WeekNumber,
                payrollRun.Status
            })
            .HasDatabaseName("IX_PayrollRuns_Historial");
    }
}

/// <summary>Mapeo de la linea de una ejecucion de nomina.</summary>
public sealed class PayrollRunLineConfiguration : IEntityTypeConfiguration<PayrollRunLine>
{
    private const int PERSON_NAME_MAXIMUM_LENGTH = 200;
    private const int SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH = 20;
    private const int DESCRIPTION_MAXIMUM_LENGTH = 150;
    private const int FORMULA_MAXIMUM_LENGTH = 300;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PayrollRunLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PayrollRunLines");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.EmployeeFullName)
            .IsRequired()
            .HasMaxLength(PERSON_NAME_MAXIMUM_LENGTH);

        builder.Property(line => line.SocialSecurityNumber)
            .IsRequired()
            .HasMaxLength(SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH);

        builder.Property(line => line.EmployeeType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(line => line.EmployeeTypeDescription)
            .IsRequired()
            .HasMaxLength(DESCRIPTION_MAXIMUM_LENGTH);

        builder.Property(line => line.DepartmentName)
            .IsRequired()
            .HasMaxLength(DESCRIPTION_MAXIMUM_LENGTH);

        builder.Property(line => line.WeeklyPayment)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);

        builder.Property(line => line.PaymentFormula)
            .IsRequired()
            .HasMaxLength(FORMULA_MAXIMUM_LENGTH);

        builder.Property(line => line.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(line => line.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.HasOne(line => line.PayrollRun)
            .WithMany(payrollRun => payrollRun.Lines)
            .HasForeignKey(line => line.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // El empleado puede eliminarse despues: la referencia queda en nulo pero la
        // instantanea del pago se conserva.
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(line => line.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(line => line.PayrollRunId)
            .HasDatabaseName("IX_PayrollRunLines_PayrollRunId");
    }
}

/// <summary>Mapeo del componente de calculo de una linea de nomina.</summary>
public sealed class PayrollRunLineComponentConfiguration
    : IEntityTypeConfiguration<PayrollRunLineComponent>
{
    private const int CONCEPT_MAXIMUM_LENGTH = 100;
    private const int DETAIL_MAXIMUM_LENGTH = 250;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PayrollRunLineComponent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PayrollRunLineComponents");

        builder.HasKey(component => component.Id);

        builder.Property(component => component.Concept)
            .IsRequired()
            .HasMaxLength(CONCEPT_MAXIMUM_LENGTH);

        builder.Property(component => component.Detail)
            .IsRequired()
            .HasMaxLength(DETAIL_MAXIMUM_LENGTH);

        builder.Property(component => component.Amount)
            .HasPrecision(ColumnDefinitions.MONETARY_PRECISION, ColumnDefinitions.MONETARY_SCALE);

        builder.Property(component => component.CreatedBy)
            .IsRequired()
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.Property(component => component.UpdatedBy)
            .HasMaxLength(ColumnDefinitions.AUDIT_USER_MAXIMUM_LENGTH);

        builder.HasOne(component => component.PayrollRunLine)
            .WithMany(line => line.Components)
            .HasForeignKey(component => component.PayrollRunLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(component => component.PayrollRunLineId)
            .HasDatabaseName("IX_PayrollRunLineComponents_PayrollRunLineId");
    }
}
