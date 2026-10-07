using System.ComponentModel.DataAnnotations;

namespace Backend.API.Validation;

// Política de contraseñas: entre 10 y 128 caracteres, con al menos una letra y un número.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    public StrongPasswordAttribute()
        : base($"La contraseña debe tener entre {MinLength} y {MaxLength} caracteres e incluir al menos una letra y un número.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true; // [Required] decide si es obligatoria
        }

        return value is string password && IsStrong(password);
    }

    public static bool IsStrong(string password)
    {
        return password.Length is >= MinLength and <= MaxLength
            && password.Any(char.IsLetter)
            && password.Any(char.IsDigit)
            && !string.IsNullOrWhiteSpace(password);
    }
}
