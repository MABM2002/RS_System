namespace Reports.PdfEngine.Exceptions;

/// <summary>
/// Thrown when the PDF rendering process fails.
/// </summary>
public sealed class PdfRenderException : Exception
{
    public PdfRenderException(string message)
        : base(message) { }

    public PdfRenderException(string message, Exception innerException)
        : base(message, innerException) { }
}
