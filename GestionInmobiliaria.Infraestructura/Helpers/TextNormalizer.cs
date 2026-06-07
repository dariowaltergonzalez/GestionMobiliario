using System.Globalization;

namespace GestionInmobiliaria.Infraestructura.Helpers;

public static class TextNormalizer
{
    private static readonly TextInfo _textInfo = new CultureInfo("es-AR").TextInfo;

    /// <summary>
    /// Title Case: "villa mercedes" → "Villa Mercedes". Para nombres, direcciones, barrios.
    /// </summary>
    public static string? TitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return _textInfo.ToTitleCase(value.Trim().ToLower());
    }

    /// <summary>
    /// Lowercase y trim. Para emails.
    /// </summary>
    public static string? Lower(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return value.Trim().ToLower();
    }
}
