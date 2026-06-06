namespace Reports.PdfEngine.Abstractions;

/// <summary>
/// Resolves an HTML template by its logical name.
/// </summary>
public interface ITemplateResolver
{
    /// <summary>
    /// Returns the raw HTML content of the template.
    /// </summary>
    /// <param name="templateName">Logical name of the template (without extension).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw HTML template string.</returns>
    Task<string> ResolveAsync(string templateName, CancellationToken ct = default);
}
