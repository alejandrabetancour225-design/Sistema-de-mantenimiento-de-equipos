namespace Backend.API.Models;

// Expresión coloquial y su significado técnico (glosario de modismos, español de Colombia).
// La IA lo usa para entender descripciones informales ("se fundió", "se pega").
// Las entradas base vienen de Data/Seed/glosario_colombia.json; Técnicos y Administradores agregan más.
public class SlangTerm
{
    public Guid Id { get; set; }
    public string Expression { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public bool IsBase { get; set; }
    public bool Active { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
