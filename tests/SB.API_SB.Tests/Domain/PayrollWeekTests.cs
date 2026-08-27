using SB.API_SB.Domain.Exceptions;
using SB.API_SB.Domain.ValueObjects;
using Xunit;

namespace SB.API_SB.Tests.Domain;

/// <summary>
/// Pruebas de la semana de nomina.
/// </summary>
/// <remarks>
/// La identidad de la semana es la clave que impide pagar dos veces el mismo
/// periodo, por lo que se verifica que dos fechas distintas de la misma semana
/// produzcan exactamente la misma semana, y que los limites de fin de ano se
/// resuelvan segun la norma ISO 8601.
/// </remarks>
public sealed class PayrollWeekTests
{
    [Fact]
    public void FromDate_DosFechasDeLaMismaSemana_ProducenLaMismaSemana()
    {
        // Lunes y domingo de la misma semana ISO.
        PayrollWeek fromMonday = PayrollWeek.FromDate(new DateOnly(2026, 8, 24));
        PayrollWeek fromSunday = PayrollWeek.FromDate(new DateOnly(2026, 8, 30));

        Assert.Equal(fromMonday, fromSunday);
        Assert.Equal(fromMonday.Year, fromSunday.Year);
        Assert.Equal(fromMonday.WeekNumber, fromSunday.WeekNumber);
    }

    [Fact]
    public void StartDate_SiempreEsLunesYEndDateSiempreEsDomingo()
    {
        PayrollWeek week = PayrollWeek.FromDate(new DateOnly(2026, 8, 26));

        Assert.Equal(DayOfWeek.Monday, week.StartDate.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, week.EndDate.DayOfWeek);
        Assert.Equal(6, week.EndDate.DayNumber - week.StartDate.DayNumber);
    }

    [Fact]
    public void FromDate_FinDeAno_UsaElAnoIsoYNoElDelCalendario()
    {
        // El 31 de diciembre de 2025 pertenece a la semana 1 del ano ISO 2026.
        PayrollWeek week = PayrollWeek.FromDate(new DateOnly(2025, 12, 31));

        Assert.Equal(2026, week.Year);
        Assert.Equal(1, week.WeekNumber);
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2101, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 54)]
    [InlineData(2026, -1)]
    public void Create_AnoOSemanaFueraDeRango_LanzaExcepcionDeReglaDeNegocio(
        int year,
        int weekNumber)
    {
        Assert.Throws<BusinessRuleViolationException>(() => PayrollWeek.Create(year, weekNumber));
    }

    [Fact]
    public void Create_SemanaCincuentaYTresEnUnAnoDeCincuentaYDos_LanzaExcepcion()
    {
        // 2026 tiene 53 semanas ISO; 2025 tiene 52.
        BusinessRuleViolationException exception =
            Assert.Throws<BusinessRuleViolationException>(() => PayrollWeek.Create(2025, 53));

        Assert.Contains("52 semanas", exception.Message);
        Assert.NotNull(PayrollWeek.Create(2026, 53));
    }

    [Fact]
    public void Previous_YLuegoNext_DevuelveLaSemanaOriginal()
    {
        PayrollWeek week = PayrollWeek.Create(2026, 35);

        Assert.Equal(week, week.Previous().Next());
        Assert.Equal(34, week.Previous().WeekNumber);
        Assert.Equal(36, week.Next().WeekNumber);
    }

    [Fact]
    public void Previous_EnLaPrimeraSemana_RetrocedeAlAnoAnterior()
    {
        PayrollWeek firstWeek = PayrollWeek.Create(2026, 1);
        PayrollWeek previousWeek = firstWeek.Previous();

        Assert.Equal(2025, previousWeek.Year);
        Assert.Equal(52, previousWeek.WeekNumber);
    }

    [Fact]
    public void Label_UsaFormatoEstableEIndependienteDeLaCultura()
    {
        Assert.Equal("2026-S35", PayrollWeek.Create(2026, 35).Label);
        Assert.Equal("2026-S05", PayrollWeek.Create(2026, 5).Label);
    }
}
