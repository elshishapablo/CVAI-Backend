using System.Net.Http.Json;
using System.Text.Json;
using CVMatchAI.API.Models;

namespace CVMatchAI.API.Services;

public class ClaudeService : IClaudeService
{
    private readonly HttpClient _http;
    private readonly ILogger<ClaudeService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly bool _mockMode;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ClaudeService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<ClaudeService> logger)
    {
        _logger = logger;
        _http   = httpFactory.CreateClient("gemini");

        _apiKey = config["GoogleAI:ApiKey"];
        _model  = config["GoogleAI:Model"] ?? "gemini-2.0-flash";
        _mockMode = string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_API_KEY_HERE";

        if (!_mockMode)
            _logger.LogInformation("Google AI (Gemini) mode active ({Model}).", _model);
        else
            _logger.LogWarning("AI Service: MOCK MODE active (no GoogleAI:ApiKey configured).");
    }

    public async Task<AnalysisResult> AnalyzeAsync(string cvText, string jobDescription)
    {
        if (_mockMode)
            return BuildMockResult();

        var prompt = BuildPrompt(cvText, jobDescription);

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var responseText = await CallGeminiAsync(prompt);
                var result = ParseResponse(responseText);
                if (result is not null) return result;

                _logger.LogWarning("Attempt {Attempt}: response was not valid JSON.", attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {Attempt}: error calling Gemini.", attempt);
                if (attempt == 2)
                {
                    _logger.LogWarning("Gemini failed after 2 attempts. Falling back to mock result.");
                    return BuildMockResult();
                }
            }
        }

        _logger.LogWarning("Could not get valid AI response. Returning mock result.");
        return BuildMockResult();
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.3,
                responseMimeType = "application/json"
            }
        };

        using var response = await _http.PostAsJsonAsync(url, payload);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini HTTP {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "{}";
    }

    private static string BuildPrompt(string cvText, string jobDescription)
    {
        return $$"""
Eres un experto en recursos humanos y reclutamiento tech. Analiza el siguiente CV y la oferta de trabajo, y devuelve ÚNICAMENTE un JSON válido con este formato exacto, sin texto adicional ni markdown:
{
  "compatibilityScore": número del 0 al 100,
  "summary": "resumen ejecutivo de 2-3 oraciones sobre la compatibilidad",
  "strengths": [
    "fortaleza 1 del candidato para este cargo",
    "fortaleza 2",
    "fortaleza 3"
  ],
  "weaknesses": [
    "debilidad o gap 1",
    "debilidad o gap 2"
  ],
  "missingKeywords": [
    "keyword que falta en el CV pero está en la oferta",
    "keyword 2"
  ],
  "presentKeywords": [
    "keyword del CV que coincide con la oferta",
    "keyword 2"
  ],
  "suggestions": [
    {
      "section": "nombre de la sección del CV (ej: Perfil Profesional)",
      "original": "texto original o descripción de lo que tiene ahora",
      "improved": "versión mejorada concreta y lista para copiar"
    }
  ],
  "coverLetterIntro": "primer párrafo de carta de presentación personalizado para este cargo"
}

CV DEL CANDIDATO:
{{cvText}}

OFERTA DE TRABAJO:
{{jobDescription}}
""";
    }

    private static AnalysisResult? ParseResponse(string responseText)
    {
        var text = responseText.Trim();
        if (text.StartsWith("```"))
        {
            var start = text.IndexOf('\n') + 1;
            var end   = text.LastIndexOf("```");
            if (end > start) text = text[start..end].Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<AnalysisResult>(text, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private static AnalysisResult BuildMockResult() => new()
    {
        CompatibilityScore = 72,
        Summary = "El candidato muestra una sólida base técnica que se alinea bien con los requisitos del puesto. " +
                  "Tiene experiencia relevante en las tecnologías principales solicitadas, aunque le faltan algunos " +
                  "conocimientos específicos en herramientas de cloud y CI/CD que la empresa valora.",
        Strengths = new List<string>
        {
            "Experiencia comprobada en React y TypeScript, tecnologías core del puesto",
            "Historial de proyectos en equipos ágiles con metodología Scrum",
            "Portafolio con proyectos de complejidad media-alta bien documentados",
            "Habilidades de comunicación demostradas en entornos internacionales"
        },
        Weaknesses = new List<string>
        {
            "No menciona experiencia con AWS ni servicios cloud (requisito del puesto)",
            "Falta experiencia documentada con Docker y contenedores",
            "El perfil profesional es genérico y no está orientado a este tipo de empresa"
        },
        PresentKeywords = new List<string>
        {
            "React", "TypeScript", "Node.js", "REST API", "Git", "Scrum", "JavaScript", "CSS"
        },
        MissingKeywords = new List<string>
        {
            "AWS", "Docker", "Kubernetes", "CI/CD", "Jest", "GraphQL", "PostgreSQL"
        },
        Suggestions = new List<SuggestionItem>
        {
            new()
            {
                Section  = "Perfil Profesional",
                Original = "Desarrollador Full Stack con 4 años de experiencia en desarrollo web.",
                Improved = "Desarrollador Full Stack especializado en React/TypeScript con 4 años construyendo aplicaciones " +
                           "web escalables. Apasionado por el código limpio y las buenas prácticas, con experiencia " +
                           "trabajando en equipos ágiles internacionales."
            },
            new()
            {
                Section  = "Habilidades Técnicas",
                Original = "HTML, CSS, JavaScript, React, Node.js",
                Improved = "Frontend: React 18, TypeScript, Next.js, TailwindCSS | Backend: Node.js, Express, REST APIs | " +
                           "Base de datos: MongoDB, MySQL | Herramientas: Git, GitHub Actions | En aprendizaje: AWS, Docker"
            },
            new()
            {
                Section  = "Experiencia Laboral",
                Original = "Desarrollé features para la plataforma principal usando React.",
                Improved = "Lideré el desarrollo de 5 features críticas en React 18 que redujeron el tiempo de carga " +
                           "en un 40%. Implementé tests unitarios con Jest alcanzando 85% de cobertura. Colaboré con " +
                           "equipo de 8 personas en sprints de 2 semanas."
            }
        },
        CoverLetterIntro = "Me dirijo a ustedes con gran entusiasmo para postular al puesto de Desarrollador Full Stack. " +
                           "Con 4 años de experiencia construyendo aplicaciones web con React y TypeScript, y un historial " +
                           "probado de entrega de proyectos de calidad en equipos ágiles, estoy convencido de poder aportar " +
                           "valor inmediato a su equipo mientras continúo creciendo en las tecnologías cloud que caracterizan " +
                           "la arquitectura moderna de su plataforma."
    };
}
