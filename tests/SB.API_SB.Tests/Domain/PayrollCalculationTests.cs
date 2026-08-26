using SB.API_SB.Domain.Entities;
using SB.API_SB.Domain.Enums;
using SB.API_SB.Domain.ValueObjects;
using Xunit;

namespace SB.API_SB.Tests.Domain;

/// <summary>
/// Pruebas del calculo de pago semanal de cada tipo de empleado.
/// </summary>
/// <remarks>
/// Es la regla de negocio mas critica del sistema, por lo que se verifica cada
/// formula por separado y, sobre todo, el limite de las 40 horas: es el punto
/// donde un error de comparacion se traduce en un pago incorrecto.
/// </remarks>
public sealed class PayrollCalculationTests
{
    [Fact]
    public void CalculateWeeklyPayment_EmpleadoAsalariado_DevuelveElSalarioSemanal()
    {
        SalariedEmployee employee = new()
        {
            FirstName = "Ana",
            PaternalLastName = "Martinez",
            SocialSecurityNumber = "001-0000001-1",
            WeeklySalary = 35_000m
        };

        decimal weeklyPayment = employee.CalculateWeeklyPayment();

        Assert.Equal(35_000m, weeklyPayment);
        Assert.Equal(EmployeeType.Salaried, employee.Type);
    }

    [Theory]
    [InlineData(100, 0, 0)]
    [InlineData(100, 20, 2_000)]
    [InlineData(100, 40, 4_000)]
    [InlineData(100, 41, 4_150)]
    [InlineData(450, 46, 22_050)]
    [InlineData(300, 50, 16_500)]
    public void CalculateWeeklyPayment_EmpleadoPorHoras_AplicaRecargoSoloSobreLasHorasExtras(
        decimal hourlyWage,
        decimal hoursWorked,
        decimal expectedPayment)
    {
        HourlyEmployee employee = new()
        {
            PaternalLastName = "Rodriguez",
            SocialSecurityNumber = "001-0000002-2",
            HourlyWage = hourlyWage,
            HoursWorked = hoursWorked
        };

        decimal weeklyPayment = employee.CalculateWeeklyPayment();

        Assert.Equal(expectedPayment, weeklyPayment);
    }

    [Fact]
    public void CalculateWeeklyPayment_EmpleadoPorHoras_SeparaHorasOrdinariasYExtras()
    {
        HourlyEmployee employee = new()
        {
            PaternalLastName = "Rodriguez",
            SocialSecurityNumber = "001-0000002-2",
            HourlyWage = 450m,
            HoursWorked = 46m
        };

        Assert.Equal(40m, employee.RegularHours);
        Assert.Equal(6m, employee.OvertimeHours);
    }

    [Fact]
    public void CalculateWeeklyPayment_EmpleadoPorComision_MultiplicaVentasPorTarifa()
    {
        CommissionEmployee employee = new()
        {
            FirstName = "Luis",
            PaternalLastName = "Perez",
            SocialSecurityNumber = "001-0000003-3",
            GrossSales = 250_000m,
            CommissionRate = 0.08m
        };

        decimal weeklyPayment = employee.CalculateWeeklyPayment();

        Assert.Equal(20_000m, weeklyPayment);
    }

    [Fact]
    public void CalculateWeeklyPayment_EmpleadoAsalariadoPorComision_SumaComisionSalarioBaseEIncentivo()
    {
        BaseSalariedCommissionEmployee employee = new()
        {
            FirstName = "Carmen",
            PaternalLastName = "Guzman",
            SocialSecurityNumber = "001-0000004-4",
            GrossSales = 180_000m,
            CommissionRate = 0.05m,
            BaseSalary = 20_000m
        };

        decimal weeklyPayment = employee.CalculateWeeklyPayment();

        // 9,000 de comision + 20,000 de salario base + 2,000 de incentivo (10%).
        Assert.Equal(31_000m, weeklyPayment);
    }

    [Fact]
    public void BuildPaymentBreakdown_ParaTodosLosTipos_LosComponentesSumanElTotal()
    {
        Employee[] employees =
        {
            new SalariedEmployee { WeeklySalary = 12_345.67m },
            new HourlyEmployee { HourlyWage = 137.55m, HoursWorked = 47.5m },
            new CommissionEmployee { GrossSales = 98_765.43m, CommissionRate = 0.0725m },
            new BaseSalariedCommissionEmployee
            {
                GrossSales = 55_555.55m,
                CommissionRate = 0.0333m,
                BaseSalary = 9_876.54m
            }
        };

        foreach (Employee employee in employees)
        {
            PaymentBreakdown breakdown = employee.BuildPaymentBreakdown();
            decimal sumOfComponents = breakdown.Components.Sum(component => component.Amount);

            Assert.Equal(breakdown.TotalAmount, employee.CalculateWeeklyPayment());
            Assert.Equal(breakdown.TotalAmount, sumOfComponents);
            Assert.False(string.IsNullOrWhiteSpace(breakdown.Formula));
            Assert.NotEmpty(breakdown.Components);
        }
    }

    [Fact]
    public void FullName_EmpleadoSinPrimerNombre_DevuelveSoloElApellido()
    {
        HourlyEmployee employee = new()
        {
            PaternalLastName = "Rodriguez",
            HourlyWage = 100m,
            HoursWorked = 10m
        };

        Assert.Equal("Rodriguez", employee.FullName);
    }
}
