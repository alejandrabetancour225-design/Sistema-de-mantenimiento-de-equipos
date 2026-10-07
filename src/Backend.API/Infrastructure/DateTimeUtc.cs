namespace Backend.API.Infrastructure;

public static class DateTimeUtc
{
    // PostgreSQL (timestamp with time zone) solo acepta DateTime en UTC.
    // Las fechas sin zona horaria que llegan en el JSON se interpretan como UTC.
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? Normalize(DateTime? value) => value is null ? null : Normalize(value.Value);
}
