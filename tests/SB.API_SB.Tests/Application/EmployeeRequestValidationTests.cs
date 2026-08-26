using FluentValidation.Results;
using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Application.Validators.Employees;
using SB.API_SB.Domain.Enums;
using Xunit;

namespace SB.API_SB.Tests.Application;

/// <summary>
/// Pruebas de las validaciones condicionales por tipo de empleado.
/// </summary>
/// <remarks>
/// El contrato de entrada es unico para los cuatro tipos, por lo que la
/// correccion del sistema depende de que las reglas se activen exactamente para
/// el tipo que corresponde. Estas pruebas fijan ese comportamiento.
/// </remarks>
public sealed class EmployeeRequestValidationTests
{
    private static readonly Guid DEPARTMENT_ID =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly CreateEmployeeRequestValidator validator = new();

    [Fact]
    public void Validate_EmpleadoAsalariadoCompleto_EsValido()
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Salaried);
        request.FirstName = "Ana";
        request.WeeklySalary = 35_000m;

        ValidationResult result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmpleadoAsalariadoSinSalarioSemanal_EsInvalido()
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Salaried);
        request.FirstName = "Ana";

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == nameof(CreateEmployeeRequest.WeeklySalary));
    }

    [Fact]
    public void Validate_EmpleadoPorHorasSinPrimerNombre_EsValido()
    {
        // La especificacion solo exige el primer nombre para los otros tres tipos.
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Hourly);
        request.HourlyWage = 300m;
        request.HoursWorked = 45m;

        ValidationResult result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmpleadoPorComisionSinPrimerNombre_EsInvalido()
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Commission);
        request.GrossSales = 100_000m;
        request.CommissionRate = 0.05m;

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == nameof(CreateEmployeeRequest.FirstName));
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(0)]
    [InlineData(-0.1)]
    public void Validate_TarifaDeComisionFueraDeRango_EsInvalido(decimal commissionRate)
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Commission);
        request.FirstName = "Luis";
        request.GrossSales = 100_000m;
        request.CommissionRate = commissionRate;

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == nameof(CreateEmployeeRequest.CommissionRate));
    }

    [Fact]
    public void Validate_HorasTrabajadasSuperioresALasDeUnaSemana_EsInvalido()
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Hourly);
        request.HourlyWage = 300m;
        request.HoursWorked = 200m;

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == nameof(CreateEmployeeRequest.HoursWorked));
    }

    [Fact]
    public void Validate_EmpleadoAsalariadoPorComisionSinSalarioBase_EsInvalido()
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.BaseSalariedCommission);
        request.FirstName = "Carmen";
        request.GrossSales = 180_000m;
        request.CommissionRate = 0.05m;

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == nameof(CreateEmployeeRequest.BaseSalary));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("001 0000001 1")]
    public void Validate_NumeroDeSeguroSocialInvalido_EsInvalido(string socialSecurityNumber)
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Salaried);
        request.FirstName = "Ana";
        request.WeeklySalary = 35_000m;
        request.SocialSecurityNumber = socialSecurityNumber;

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure =>
                failure.PropertyName == nameof(CreateEmployeeRequest.SocialSecurityNumber));
    }

    [Fact]
    public void Validate_SinDepartamento_EsInvalido()
    {
        CreateEmployeeRequest request = BuildBaseRequest(EmployeeType.Salaried);
        request.FirstName = "Ana";
        request.WeeklySalary = 35_000m;
        request.DepartmentId = Guid.Empty;

        ValidationResult result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == nameof(CreateEmployeeRequest.DepartmentId));
    }

    private static CreateEmployeeRequest BuildBaseRequest(EmployeeType employeeType) => new()
    {
        Type = employeeType,
        PaternalLastName = "Martinez",
        SocialSecurityNumber = "001-0000001-1",
        DepartmentId = DEPARTMENT_ID,
        Status = EmployeeStatus.Active
    };
}
