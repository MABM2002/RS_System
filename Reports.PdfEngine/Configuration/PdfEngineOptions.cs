namespace Reports.PdfEngine.Configuration;

/// <summary>
/// Configuration options for the PDF reporting engine.
/// </summary>
public sealed class PdfEngineOptions
{
    /// <summary>
    /// Section name in appsettings.json.
    /// </summary>
    public const string SectionName = "PdfEngine";

    /// <summary>
    /// Absolute or relative path to the directory containing HTML template files.
    /// </summary>
    public string TemplatesPath { get; set; } = "Templates/Reports";

    /// <summary>
    /// File extension for template files (including the dot).
    /// </summary>
    public string TemplateExtension { get; set; } = ".html";

    /// <summary>
    /// PDF paper format (e.g., "A4", "Letter").
    /// </summary>
    public string PaperFormat { get; set; } = "A4";

    /// <summary>
    /// Whether to print background graphics in the PDF.
    /// </summary>
    public bool PrintBackground { get; set; } = true;

    /// <summary>
    /// Top margin (CSS units, e.g., "10mm").
    /// </summary>
    public string MarginTop { get; set; } = "10mm";

    /// <summary>
    /// Bottom margin (CSS units).
    /// </summary>
    public string MarginBottom { get; set; } = "10mm";

    /// <summary>
    /// Left margin (CSS units).
    /// </summary>
    public string MarginLeft { get; set; } = "10mm";

    /// <summary>
    /// Right margin (CSS units).
    /// </summary>
    public string MarginRight { get; set; } = "10mm";

    /// <summary>
    /// Timeout in milliseconds for the browser to generate the PDF.
    /// </summary>
    public int BrowserTimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Whether the PDF should be in landscape orientation.
    /// </summary>
    public bool Landscape { get; set; } = false;

    /// <summary>
    /// Whether to cache parsed templates in memory.
    /// </summary>
    public bool EnableTemplateCache { get; set; } = true;
}
