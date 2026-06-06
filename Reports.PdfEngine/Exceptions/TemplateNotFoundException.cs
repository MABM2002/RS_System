namespace Reports.PdfEngine.Exceptions;

/// <summary>
/// Thrown when a requested report template cannot be found.
/// </summary>
public sealed class TemplateNotFoundException : Exception
{
    public string TemplateName { get; }

    public TemplateNotFoundException(string templateName)
        : base($"Report template '{templateName}' was not found.")
    {
        TemplateName = templateName;
    }

    public TemplateNotFoundException(string templateName, string searchPath)
        : base($"Report template '{templateName}' was not found at path '{searchPath}'.")
    {
        TemplateName = templateName;
    }
}
