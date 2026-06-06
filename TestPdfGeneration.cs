// Quick integration test for Playwright PDF generation on Fedora
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reports.PdfEngine.Abstractions;
using Reports.PdfEngine.Configuration;
using Reports.PdfEngine.DependencyInjection;
using Reports.PdfEngine.Rendering;
using Reports.PdfEngine.Templates;

var services = new ServiceCollection();

// Add logging
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

// Configure PDF engine options
services.Configure<PdfEngineOptions>(options =>
{
    options.TemplatesPath = "RS_system/Templates/Reports";
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

// Add PDF reporting services
services.AddScoped<ITemplateResolver, FileTemplateResolver>();
services.AddScoped<IHtmlRenderer, ScribanHtmlRenderer>();
services.AddSingleton<PlaywrightPdfEngine>();
services.AddScoped<IReportGenerator, ReportGenerator>();

var provider = services.BuildServiceProvider();

try
{
    Console.WriteLine("Testing Playwright PDF Engine on Fedora...");
    
    // Get the PDF engine
    var pdfEngine = provider.GetRequiredService<PlaywrightPdfEngine>();
    
    // Simple HTML test
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

    Console.WriteLine("Generating PDF...");
    var pdfBytes = await pdfEngine.HtmlToPdfAsync(htmlContent);
    
    Console.WriteLine($"PDF generated successfully! Size: {pdfBytes.Length} bytes");
    
    // Save to file for verification
    var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "test-output.pdf");
    await File.WriteAllBytesAsync(outputPath, pdfBytes);
    Console.WriteLine($"PDF saved to: {outputPath}");
    
    // Also test the full report generator pipeline
    Console.WriteLine("\nTesting full ReportGenerator pipeline...");
    var reportGenerator = provider.GetRequiredService<IReportGenerator>();
    
    var testModel = new 
    {
        Title = "Test Report",
        Date = DateTime.Now,
        Items = new[] { "Item 1", "Item 2", "Item 3" }
    };
    
    // This will use the DiarioFinanciero template if it exists
    try 
    {
        var reportPdf = await reportGenerator.GenerateAsync("DiarioFinanciero", testModel);
        Console.WriteLine($"Report PDF generated! Size: {reportPdf.Length} bytes");
        
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "test-report.pdf");
        await File.WriteAllBytesAsync(reportPath, reportPdf);
        Console.WriteLine($"Report PDF saved to: {reportPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Report generation failed (expected if template needs specific model): {ex.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    // Cleanup
    if (provider is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync();
    }
    else if (provider is IDisposable disposable)
    {
        disposable.Dispose();
    }
}

Console.WriteLine("\nTest completed!");