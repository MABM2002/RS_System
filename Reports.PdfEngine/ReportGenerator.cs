using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Reports.PdfEngine.Abstractions;
using Reports.PdfEngine.Exceptions;
using Reports.PdfEngine.Rendering;

namespace Reports.PdfEngine;

/// <summary>
/// Orchestrates the full report generation pipeline:
/// Template Resolution → HTML Rendering (Scriban) → PDF Conversion (Playwright).
/// </summary>
public sealed class ReportGenerator : IReportGenerator
{
    private readonly ITemplateResolver _templateResolver;
    private readonly IHtmlRenderer _htmlRenderer;
    private readonly PlaywrightPdfEngine _pdfEngine;
    private readonly ILogger<ReportGenerator> _logger;

    public ReportGenerator(
        ITemplateResolver templateResolver,
        IHtmlRenderer htmlRenderer,
        PlaywrightPdfEngine pdfEngine,
        ILogger<ReportGenerator> logger)
    {
        _templateResolver = templateResolver;
        _htmlRenderer = htmlRenderer;
        _pdfEngine = pdfEngine;
        _logger = logger;
    }

    public async Task<byte[]> GenerateAsync<T>(string templateName, T model, CancellationToken ct = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(model);

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Generating PDF report for template '{TemplateName}'", templateName);

            // 1. Resolve the HTML template
            var templateContent = await _templateResolver
                .ResolveAsync(templateName, ct)
                .ConfigureAwait(false);

            // 2. Render data into HTML via Scriban
            var renderedHtml = await _htmlRenderer
                .RenderAsync(templateContent, model, ct)
                .ConfigureAwait(false);

            // 3. Convert HTML → PDF via Playwright
            var pdfBytes = await _pdfEngine
                .HtmlToPdfAsync(renderedHtml, ct)
                .ConfigureAwait(false);

            sw.Stop();
            _logger.LogInformation(
                "PDF report '{TemplateName}' generated in {Elapsed}ms ({Size} bytes)",
                templateName, sw.ElapsedMilliseconds, pdfBytes.Length);

            return pdfBytes;
        }
        catch (TemplateNotFoundException)
        {
            throw;
        }
        catch (PdfRenderException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PDF generation cancelled for template '{TemplateName}'", templateName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating PDF for template '{TemplateName}'", templateName);
            throw new PdfRenderException(
                $"Unexpected error generating report '{templateName}'.", ex);
        }
    }

    public async Task<Stream> GenerateStreamAsync<T>(string templateName, T model, CancellationToken ct = default)
        where T : class
    {
        var pdfBytes = await GenerateAsync(templateName, model, ct).ConfigureAwait(false);
        return new MemoryStream(pdfBytes, writable: false);
    }
}
