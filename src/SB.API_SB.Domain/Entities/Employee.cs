using SB.API_SB.Domain.Common;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;

namespace SB.API_SB.Domain.Entities;

/// <summary>
/// Empleado del sistema. Es una clase abstracta porque un empleado siempre
/// pertenece a un tipo concreto de contrato, y cada tipo calcula su pago de
/// forma distinta.
/// </summary>
/// <remarks>
/// El calculo de pago se resuelve por polimorfismo: la clase base declara
/// <see cref="CalculateWeeklyPayment"/> como abstracto y cada subclase aporta su
/// propia formula. De esta manera se cumple el Principio Abierto/Cerrado (OCP):
/// agregar un nuevo tipo de empleado consiste en crear una nueva subclase, sin
/// modificar ni la clase base ni los servicios que ya consumen la jerarquia.
/// </remarks>
public abstract class Employee : AuditableEntity
{
    /// <summary>Primer nombre del empleado.</summary>
    /// <remarks>
    /// Es opcional en la clase base porque la especificacion funcional solo
    /// solicita capturarlo para los tipos asalariado, por comision y asalariado
    /// por comision. La obligatoriedad por tipo se aplica en las validaciones.
    /// </remarks>
    public string? FirstName { get; set; }

    /// <summary>Apellido paterno del empleado. Obligatorio para todos los tipos.</summary>
    public string PaternalLastName { get; set; } = string.Empty;

    /// <summary>Numero de seguro social. Identifica al empleado de forma unica.</summary>
    public string SocialSecurityNumber { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de la entidad gubernamental que emplea a la persona.
    /// </summary>
    /// <remarks>
    /// No hay propiedad de navegacion hacia la entidad gubernamental porque los
    /// dos registros viven en almacenes distintos: el empleado en la base de datos
    /// relacional y la entidad en el archivo de texto plano. La asociacion se
    /// resuelve en la capa de servicios, que consulta el catalogo y valida que la
    /// entidad exista antes de aceptar el empleado.
    /// </remarks>
    public Guid GovernmentEntityId { get; set; }

    /// <summary>Identificador del departamento al que pertenece el empleado.</summary>
    public Guid DepartmentId { get; set; }

    /// <summary>Departamento al que pertenece el empleado.</summary>
    public Department? Department { get; set; }

    /// <summary>Estado laboral del empleado.</summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>Tipo de contrato del empleado. Lo determina la subclase concreta.</summary>
    public abstract EmployeeType Type { get; }

    /// <summary>Nombre completo del empleado, listo para mostrarse en pantalla.</summary>
    public string FullName =>
        string.IsNullOrWhiteSpace(FirstName)
            ? PaternalLastName
            : $"{FirstName} {PaternalLastName}";

    /// <summary>
    /// Calcula el pago semanal del empleado segun las reglas propias de su tipo.
    /// </summary>
    /// <returns>Monto a pagar en la semana, redondeado a dos decimales.</returns>
    public abstract decimal CalculateWeeklyPayment();

    /// <summary>
    /// Construye el desglose del calculo de pago, utilizado por el reporte
    /// semanal para explicar como se obtuvo el monto.
    /// </summary>
    /// <returns>Desglose inmutable del calculo.</returns>
    public abstract PaymentBreakdown BuildPaymentBreakdown();

    /// <summary>
    /// Redondea un importe monetario a la cantidad de decimales definida por el
    /// dominio, usando redondeo comercial (away from zero).
    /// </summary>
    /// <param name="amount">Importe a redondear.</param>
    /// <returns>Importe redondeado.</returns>
    protected static decimal RoundCurrency(decimal amount) =>
        Math.Round(amount, PayrollConstants.CURRENCY_DECIMAL_PLACES, MidpointRounding.AwayFromZero);
}
