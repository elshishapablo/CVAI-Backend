using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using CVMatchAI.API.Models;

namespace CVMatchAI.API.Services;

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeService> _logger;
    private readonly bool _mockMode;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ClaudeService(IConfiguration config, ILogger<ClaudeService> logger)
    {
        _logger   = logger;
        _mockMode = string.IsNullOrWhiteSpace(config["Anthropic:ApiKey"]) ||
                    config["Anthropic:ApiKey"] == "YOUR_API_KEY_HERE";

        if (!_mockMode)
            _client = new AnthropicClient(config["Anthropic:ApiKey"]!);
        else
            _logger.LogWarning("ClaudeService: MODO MOCK activo (no hay API key configurada).");
    }

    public async Task<AnalysisResult> AnalyzeAsync(string cvText, string jobDescription)
    {
        // ── Modo mock: devuelve datos de ejemplo sin llamar a la API ──
        if (_mockMode)
            return await Task.FromResult(BuildMockResult());

        // ── Modo real: llama a Claude AI ──
        var prompt = BuildPrompt(cvText, jobDescription);

        // Reintento: intenta hasta 2 veces si el JSON no es válido
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var responseText = await CallClaudeAsync(prompt);
                var result       = ParseResponse(responseText);
                if (result is not null) return result;

                _logger.LogWarning("Intento {Attempt}: respuesta de Claude no es JSON válido.", attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Intento {Attempt}: error al llamar a Claude.", attempt);
                if (attempt == 2) throw;
            }
        }

        throw new InvalidOperationException("Claude no devolvió un resultado válido tras 2 intentos.");
    }

    // ──────────────────────────────────────────────────────────────
    //  Métodos privados
    // ──────────────────────────────────────────────────────────────

    private async Task<string> CallClaudeAsync(string prompt)
    {
        var request = new MessageParameters
        {
            Model     = "claude-sonnet-4-20250514",
            MaxTokens = 2000,
            Messages  = new List<Message>
            {
                new Message
                {
                    Role    = RoleType.User,
                    Content = new List<ContentBase>
                    {
                        new TextContent { Text = prompt }
                    }
                }
            }
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "{}";
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
        // Eliminar bloques de código markdown si los incluye (```json ... ```)
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

    /// <summary>
    /// Resultado de ejemplo para probar la plataforma sin API key.
    /// Simula un análisis realista de un perfil de desarrollador.
    /// </summary>
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
