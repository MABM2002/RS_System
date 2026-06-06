namespace Reports.PdfEngine.Abstractions;

/// <summary>
/// Renders an HTML template string with a data model using a template engine.
/// </summary>
public interface IHtmlRenderer
{
    /// <summary>
    /// Processes the template content with the given data model and returns the final HTML.
    /// </summary>
    /// <typeparam name="T">The type of the data model.</typeparam>
    /// <param name="templateContent">Raw HTML template with template directives.</param>
    /// <param name="model">Data model to bind.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Fully rendered HTML string.</returns>
    Task<string> RenderAsync<T>(string templateContent, T model, CancellationToken ct = default)
        where T : class;
}
