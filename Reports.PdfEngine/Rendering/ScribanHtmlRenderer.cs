using Microsoft.Extensions.Logging;
using Reports.PdfEngine.Abstractions;
using Reports.PdfEngine.Exceptions;
using Scriban;
using Scriban.Runtime;

namespace Reports.PdfEngine.Rendering;

/// <summary>
/// Renders HTML from Scriban templates with strongly-typed model binding.
/// </summary>
public sealed class ScribanHtmlRenderer : IHtmlRenderer
{
    private readonly ILogger<ScribanHtmlRenderer> _logger;

    public ScribanHtmlRenderer(ILogger<ScribanHtmlRenderer> logger)
    {
        _logger = logger;
    }

    public async Task<string> RenderAsync<T>(string templateContent, T model, CancellationToken ct = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateContent);
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            var template = Template.Parse(templateContent);

            if (template.HasErrors)
            {
                var errors = string.Join("; ", template.Messages);
                _logger.LogError("Scriban template parse errors: {Errors}", errors);
                throw new PdfRenderException($"Template parsing failed: {errors}");
            }

            var scriptObject = new ScriptObject();
            scriptObject.Import(model, renamer: member => member.Name);

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            context.MemberRenamer = member => member.Name;

            var result = await template.RenderAsync(context);

            _logger.LogDebug(
                "Template rendered successfully ({Length} chars)",
                result.Length);

            return result;
        }
        catch (PdfRenderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error rendering Scriban template");
            throw new PdfRenderException("Failed to render HTML from template.", ex);
        }
    }
}
