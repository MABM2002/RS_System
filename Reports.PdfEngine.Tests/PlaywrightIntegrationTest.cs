// Integration test for Playwright PDF generation on Fedora
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reports.PdfEngine.Abstractions;
using Reports.PdfEngine.Configuration;
using Reports.PdfEngine.DependencyInjection;
using Reports.PdfEngine.Rendering;
using Reports.PdfEngine.Templates;
using Xunit;

namespace Reports.PdfEngine.Tests;

public class PlaywrightIntegrationTest
{
    [Fact]
    public async Task HtmlToPdfAsync_WithSystemChromium_GeneratesPdf()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        services.Configure<PdfEngineOptions>(options =>
        {
            options.TemplatesPath = "Templates/Reports";
            options.TemplateExtension = ".html";
            options.PaperFormat = "A4";
            options.PrintBackground = true;
            options.MarginTop = "10mm";
            options.MarginBottom = "10mm";
            options.MarginLeft = "10mm";
            options.MarginRight = "10mm";
            options.BrowserTimeoutMs = 30000;
            options.EnableTemplateCache = true;
        });
        
        services.AddScoped<ITemplateResolver, FileTemplateResolver>();
        services.AddScoped<IHtmlRenderer, ScribanHtmlRenderer>();
        services.AddSingleton<PlaywrightPdfEngine>();
        services.AddScoped<IReportGenerator, ReportGenerator>();
        
        var provider = services.BuildServiceProvider();
        
        try
        {
            var pdfEngine = provider.GetRequiredService<PlaywrightPdfEngine>();
            
            var htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Test PDF</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; }
        h1 { color: #333; }
        .info { background: #f0f0f0; padding: 10px; border-radius: 5px; }
    </style>
</head>
<body>
    <h1>Test PDF Generation on Fedora</h1>
    <div class='info'>
        <p>This PDF was generated using Playwright with system Chromium.</p>
        <p>Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</p>
        <p>OS: " + Environment.OSVersion.ToString() + @"</p>
    </div>
</body>
</html>";

            var pdfBytes = await pdfEngine.HtmlToPdfAsync(htmlContent);
            
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
            Assert.True(pdfBytes.Length > 1000); // Should be a reasonable PDF size
            
            // Verify it's a valid PDF (starts with %PDF)
            var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
            Assert.Equal("%PDF", pdfHeader);
            
            Console.WriteLine($"PDF generated successfully! Size: {pdfBytes.Length} bytes");
        }
        finally
        {
            if (provider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
    
    [Fact]
    public async Task ReportGenerator_WithTemplate_GeneratesPdf()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        services.Configure<PdfEngineOptions>(options =>
        {
            options.TemplatesPath = "../RS_system/Templates/Reports";
            options.TemplateExtension = ".html";
            options.PaperFormat = "A4";
            options.PrintBackground = true;
            options.MarginTop = "10mm";
            options.MarginBottom = "10mm";
            options.MarginLeft = "10mm";
            options.MarginRight = "10mm";
            options.BrowserTimeoutMs = 30000;
            options.EnableTemplateCache = true;
        });
        
        services.AddScoped<ITemplateResolver, FileTemplateResolver>();
        services.AddScoped<IHtmlRenderer, ScribanHtmlRenderer>();
        services.AddSingleton<PlaywrightPdfEngine>();
        services.AddScoped<IReportGenerator, ReportGenerator>();
        
        var provider = services.BuildServiceProvider();
        
        try
        {
            var reportGenerator = provider.GetRequiredService<IReportGenerator>();
            
            // Test with a simple model that matches the template
            var testModel = new 
            {
                FechaInicio = DateTime.Now.AddDays(-7),
                FechaFin = DateTime.Now,
                Cabeceras = new List<object>()
            };
            
            var pdfBytes = await reportGenerator.GenerateAsync("DiarioFinanciero", testModel);
            
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
            
            // Verify it's a valid PDF
            var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(4).ToArray());
            Assert.Equal("%PDF", pdfHeader);
            
            Console.WriteLine($"Report PDF generated successfully! Size: {pdfBytes.Length} bytes");
        }
        finally
        {
            if (provider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}