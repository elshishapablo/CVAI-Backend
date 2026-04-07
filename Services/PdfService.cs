using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CVMatchAI.API.Services;

public class PdfService : IPdfService
{
    public string ExtractText(Stream pdfStream)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(pdfStream);

        foreach (Page page in document.GetPages())
        {
            // Unir las palabras de cada página con espacios
            var pageText = string.Join(" ", page.GetWords().Select(w => w.Text));
            sb.AppendLine(pageText);
        }

        var rawText = sb.ToString();

        // Limpiar caracteres especiales que pueden confundir a Claude
        return CleanText(rawText);
    }

    /// <summary>
    /// Elimina caracteres de control, normaliza espacios y saltos de línea
    /// para que el texto sea legible por la IA.
    /// </summary>
    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Eliminar caracteres de control (excepto saltos de línea y tabs)
        text = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", " ");

        // Normalizar múltiples espacios en blanco
        text = Regex.Replace(text, @"[ \t]+", " ");

        // Normalizar múltiples saltos de línea (máximo 2 consecutivos)
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        // Eliminar líneas que solo tengan espacios
        text = Regex.Replace(text, @"^\s+$", "", RegexOptions.Multiline);

        return text.Trim();
    }
}
