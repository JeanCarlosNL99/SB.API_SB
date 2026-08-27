using FluentValidation;
using SB.API_SB.Application.Contracts.Employees;
using SB.API_SB.Domain.Constants;
using SB.API_SB.Domain.Enums;

namespace SB.API_SB.Application.Validators.Employees;

/// <summary>
/// Validaciones comunes al alta y a la actualizacion de empleados.
/// </summary>
/// <remarks>
/// Las reglas especificas de cada tipo se agrupan en metodos privados y se
/// activan con <c>When</c>, de modo que la solicitud solo debe traer los campos
/// que su tipo realmente exige. Un campo que no corresponde al tipo se rechaza
/// explicitamente para evitar datos incoherentes en la base de datos.
/// </remarks>
/// <typeparam name="TRequest">Tipo concreto de la solicitud validada.</typeparam>
public abstract class EmployeeRequestBaseValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : EmployeeRequestBase
{
    protected EmployeeRequestBaseValidator()
    {
        ValidateCommonFields();
        ValidateSalariedEmployee();
        ValidateHourlyEmployee();
        ValidateCommissionEmployee();
        ValidateBaseSalariedCommissionEmployee();
    }

    private void ValidateCommonFields()
    {
        RuleFor(request => request.Type)
            .IsInEnum().WithMessage("El tipo de empleado indicado no es valido.");

        RuleFor(request => request.PaternalLastName)
            .NotEmpty().WithMessage("El apellido paterno es obligatorio.")
            .MaximumLength(ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El apellido paterno no puede exceder {ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.SocialSecurityNumber)
            .NotEmpty().WithMessage("El numero de seguro social es obligatorio.")
            .MinimumLength(ValidationLimits.SOCIAL_SECURITY_NUMBER_MINIMUM_LENGTH)
            .WithMessage($"El numero de seguro social debe tener al menos {ValidationLimits.SOCIAL_SECURITY_NUMBER_MINIMUM_LENGTH} caracteres.")
            .MaximumLength(ValidationLimits.SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH)
            .WithMessage($"El numero de seguro social no puede exceder {ValidationLimits.SOCIAL_SECURITY_NUMBER_MAXIMUM_LENGTH} caracteres.")
            .Matches("^[0-9A-Za-z-]+$")
            .WithMessage("El numero de seguro social solo admite letras, numeros y guiones.");

        RuleFor(request => request.CompanyId)
            .NotEqual(Guid.Empty).WithMessage("La compania es obligatoria.");

        RuleFor(request => request.DepartmentId)
            .NotEqual(Guid.Empty).WithMessage("El departamento es obligatorio.");

        RuleFor(request => request.Status)
            .IsInEnum().WithMessage("El estado del empleado indicado no es valido.");

        RuleFor(request => request.FirstName)
            .MaximumLength(ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH)
            .WithMessage($"El primer nombre no puede exceder {ValidationLimits.PERSON_NAME_MAXIMUM_LENGTH} caracteres.");

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .WithMessage("El primer nombre es obligatorio para este tipo de empleado.")
            .When(request => request.Type != EmployeeType.Hourly);
    }

    private void ValidateSalariedEmployee()
    {
        When(request => request.Type == EmployeeType.Salaried, () =>
        {
            RuleFor(request => request.WeeklySalary)
                .NotNull().WithMessage("El salario semanal es obligatorio para el empleado asalariado.")
                .GreaterThan(0m).WithMessage("El salario semanal debe ser mayor que cero.")
                .LessThanOrEqualTo(ValidationLimits.MONETARY_MAXIMUM_VALUE)
                .WithMessage($"El salario semanal no puede exceder {ValidationLimits.MONETARY_MAXIMUM_VALUE:N0}.");
        });
    }

    private void ValidateHourlyEmployee()
    {
        When(request => request.Type == EmployeeType.Hourly, () =>
        {
            RuleFor(request => request.HourlyWage)
                .NotNull().WithMessage("El sueldo por hora es obligatorio para el empleado por horas.")
                .GreaterThan(0m).WithMessage("El sueldo por hora debe ser mayor que cero.")
                .LessThanOrEqualTo(ValidationLimits.MONETARY_MAXIMUM_VALUE)
                .WithMessage($"El sueldo por hora no puede exceder {ValidationLimits.MONETARY_MAXIMUM_VALUE:N0}.");

            RuleFor(request => request.HoursWorked)
                .NotNull().WithMessage("Las horas trabajadas son obligatorias para el empleado por horas.")
                .GreaterThanOrEqualTo(0m).WithMessage("Las horas trabajadas no pueden ser negativas.")
                .LessThanOrEqualTo(PayrollConstants.MAXIMUM_WEEKLY_HOURS)
                .WithMessage($"Las horas trabajadas no pueden exceder {PayrollConstants.MAXIMUM_WEEKLY_HOURS:N0} en una semana.");
        });
    }

    private void ValidateCommissionEmployee()
    {
        When(IsCommissionBasedType, () =>
        {
            RuleFor(request => request.GrossSales)
                .NotNull().WithMessage("Las ventas brutas son obligatorias para el empleado por comision.")
                .GreaterThanOrEqualTo(0m).WithMessage("Las ventas brutas no pueden ser negativas.")
                .LessThanOrEqualTo(ValidationLimits.MONETARY_MAXIMUM_VALUE)
                .WithMessage($"Las ventas brutas no pueden exceder {ValidationLimits.MONETARY_MAXIMUM_VALUE:N0}.");

            RuleFor(request => request.CommissionRate)
                .NotNull().WithMessage("La tarifa de comision es obligatoria para el empleado por comision.")
                .GreaterThan(0m).WithMessage("La tarifa de comision debe ser mayor que cero.")
                .LessThanOrEqualTo(PayrollConstants.MAXIMUM_COMMISSION_RATE)
                .WithMessage("La tarifa de comision debe expresarse como fraccion decimal entre 0 y 1.");
        });
    }

    private void ValidateBaseSalariedCommissionEmployee()
    {
        When(request => request.Type == EmployeeType.BaseSalariedCommission, () =>
        {
            RuleFor(request => request.BaseSalary)
                .NotNull().WithMessage("El salario base es obligatorio para el empleado asalariado por comision.")
                .GreaterThan(0m).WithMessage("El salario base debe ser mayor que cero.")
                .LessThanOrEqualTo(ValidationLimits.MONETARY_MAXIMUM_VALUE)
                .WithMessage($"El salario base no puede exceder {ValidationLimits.MONETARY_MAXIMUM_VALUE:N0}.");
        });
    }

    private static bool IsCommissionBasedType(TRequest request) =>
        request.Type is EmployeeType.Commission or EmployeeType.BaseSalariedCommission;
}
