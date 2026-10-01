using Xunit;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Tests;

public class CalculoAtrasosTests
{
    [Fact]
    public void Marcacion_DentroDeTolerancia_RetornaCeroMinutosAtraso()
    {
        // Arrange: Turno 09:00 AM con 15 minutos de tolerancia (Límite 09:15 AM)
        var turno = new Turno
        {
            HoraEntrada = new TimeSpan(9, 0, 0),
            ToleranciaMinutos = 15
        };

        var horaDispositivo = new DateTime(2026, 10, 1, 9, 10, 0, DateTimeKind.Utc); // 09:10 AM

        // Act
        TimeSpan horaMarcada = horaDispositivo.TimeOfDay;
        TimeSpan horaEntradaOficial = turno.HoraEntrada;
        TimeSpan horaLimiteTolerancia = horaEntradaOficial.Add(TimeSpan.FromMinutes(turno.ToleranciaMinutos));

        int minutosAtraso = 0;
        if (horaMarcada > horaLimiteTolerancia)
        {
            TimeSpan tiempoDiferencia = horaMarcada - horaEntradaOficial;
            minutosAtraso = (int)Math.Ceiling(tiempoDiferencia.TotalMinutes);
        }

        // Assert
        Assert.Equal(0, minutosAtraso);
    }

    [Fact]
    public void Marcacion_FueraDeTolerancia_CalculaMinutosAtrasoTotalesDesdeHoraEntrada()
    {
        // Arrange: Turno 09:00 AM con 15 minutos de tolerancia. Entrada real: 09:35 AM
        var turno = new Turno
        {
            HoraEntrada = new TimeSpan(9, 0, 0),
            ToleranciaMinutos = 15
        };

        var horaDispositivo = new DateTime(2026, 10, 1, 9, 35, 0, DateTimeKind.Utc); // 09:35 AM

        // Act
        TimeSpan horaMarcada = horaDispositivo.TimeOfDay;
        TimeSpan horaEntradaOficial = turno.HoraEntrada;
        TimeSpan horaLimiteTolerancia = horaEntradaOficial.Add(TimeSpan.FromMinutes(turno.ToleranciaMinutos));

        int minutosAtraso = 0;
        if (horaMarcada > horaLimiteTolerancia)
        {
            TimeSpan tiempoDiferencia = horaMarcada - horaEntradaOficial;
            minutosAtraso = (int)Math.Ceiling(tiempoDiferencia.TotalMinutes);
        }

        // Assert: 35 minutos de retraso total respecto a la hora de entrada oficial (09:00 AM)
        Assert.Equal(35, minutosAtraso);
    }
}