using Xunit;

namespace AsistenciaCore.Tests;

public class HaversineGeofencingTests
{
    [Fact]
    public void Ubicacion_CercanaDentroDelRadio_EvaluaComoDentroDeGeocerca()
    {
        // Arrange: Geocerca en Santiago (-33.4372, -70.6506) con 200 metros de radio
        double centroLat = -33.4372;
        double centroLon = -70.6506;
        double radioMetros = 200;

        // Coordenada del trabajador a ~15 metros de distancia
        double marcacionLat = -33.4373;
        double marcacionLon = -70.6507;

        // Act
        double distanciaCalculada = CalcularDistanciaHaversine(marcacionLat, marcacionLon, centroLat, centroLon);
        bool fueraDeGeocerca = distanciaCalculada > radioMetros;

        // Assert
        Assert.False(fueraDeGeocerca);
        Assert.True(distanciaCalculada <= radioMetros);
    }

    [Fact]
    public void Ubicacion_LejanaFueraDelRadio_EvaluaComoFueraDeGeocerca()
    {
        // Arrange: Geocerca en Santiago (-33.4372, -70.6506) con 200 metros de radio
        double centroLat = -33.4372;
        double centroLon = -70.6506;
        double radioMetros = 200;

        // Coordenada a varios kilómetros de distancia
        double marcacionLat = -33.5000;
        double marcacionLon = -70.7000;

        // Act
        double distanciaCalculada = CalcularDistanciaHaversine(marcacionLat, marcacionLon, centroLat, centroLon);
        bool fueraDeGeocerca = distanciaCalculada > radioMetros;

        // Assert
        Assert.True(fueraDeGeocerca);
    }

    private static double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double RadioTierraMetros = 6371000.0;
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RadioTierraMetros * c;
    }

    private static double ToRadians(double grados) => grados * (Math.PI / 180.0);
}