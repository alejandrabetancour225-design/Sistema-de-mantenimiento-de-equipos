using System.ComponentModel.DataAnnotations;
using Backend.API.Infrastructure;

namespace Backend.API.Validation;

// Rechaza fechas futuras (con 5 minutos de tolerancia por diferencias de reloj).
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    public NotInFutureAttribute()
        : base("La fecha no puede estar en el futuro.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            DateTime date => DateTimeUtc.Normalize(date) <= DateTime.UtcNow.Add(Tolerance),
            _ => false
        };
    }
}
