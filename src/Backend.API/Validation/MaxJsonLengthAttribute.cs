using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Backend.API.Validation;

// Limita el tamaño de los campos JSON libres (características, especificaciones).
[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxJsonLengthAttribute : ValidationAttribute
{
    private readonly int _maxLength;

    public MaxJsonLengthAttribute(int maxLength)
        : base($"El campo JSON no puede superar {maxLength} caracteres.")
    {
        _maxLength = maxLength;
    }

    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            JsonElement element => element.ValueKind is JsonValueKind.Undefined
                || element.GetRawText().Length <= _maxLength,
            _ => false
        };
    }
}
