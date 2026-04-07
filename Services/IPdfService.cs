namespace CVMatchAI.API.Services;

public interface IPdfService
{
    /// <summary>
    /// Extrae el texto de un PDF recibido como stream.
    /// Limpia caracteres especiales para enviarlo a Claude.
    /// </summary>
    string ExtractText(Stream pdfStream);
}
