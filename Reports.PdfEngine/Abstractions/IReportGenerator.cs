namespace Reports.PdfEngine.Abstractions;

/// <summary>
/// Generates PDF reports by combining a named HTML template with a data model.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a PDF report as a byte array.
    /// </summary>
    /// <typeparam name="T">The type of the data model (DTO/POCO).</typeparam>
    /// <param name="templateName">Logical name of the HTML template (without extension).</param>
    /// <param name="model">The data model to bind into the template.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A byte array containing the rendered PDF.</returns>
    Task<byte[]> GenerateAsync<T>(string templateName, T model, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Generates a PDF report as a readable stream.
    /// </summary>
    Task<Stream> GenerateStreamAsync<T>(string templateName, T model, CancellationToken ct = default)
        where T : class;
}
