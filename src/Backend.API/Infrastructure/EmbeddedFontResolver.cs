using System.Reflection;
using PdfSharp.Fonts;

namespace Backend.API.Infrastructure;

// PDFsharp/MigraDoc no encuentran fuentes del sistema en Linux ni en contenedores, así que los
// PDF (reportes HU-15 y reportes con IA) fallaban fuera de Windows. Este resolutor usa la fuente
// Liberation Sans incrustada en la API (licencia SIL OFL, ver Fonts/LICENSE-LiberationSans.txt),
// métricamente compatible con Arial, para cualquier familia que se pida (Arial, Courier New...).
public sealed class EmbeddedFontResolver : IFontResolver
{
    private const string Prefix = "Backend.API.Fonts.";

    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        => new((bold, italic) switch
        {
            (true, true) => "LiberationSans-BoldItalic",
            (true, false) => "LiberationSans-Bold",
            (false, true) => "LiberationSans-Italic",
            _ => "LiberationSans-Regular"
        });

    public byte[]? GetFont(string faceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{Prefix}{faceName}.ttf");
        if (stream is null) return null;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static void Register()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new EmbeddedFontResolver();
        }
    }
}
