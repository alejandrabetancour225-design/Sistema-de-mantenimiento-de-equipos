namespace Backend.API.Infrastructure;

// Fecha "de hoy" en la zona horaria del negocio (Colombia, UTC-5 sin horario de verano).
// Se usa para reglas de negocio con fechas sin hora (DateOnly), p. ej. "la compra no puede ser futura".
public static class LocalClock
{
    private static readonly TimeZoneInfo BusinessZone = ResolveZone();

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BusinessZone));

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("America/Bogota", TimeSpan.FromHours(-5), "Colombia", "Colombia");
        }
    }
}
