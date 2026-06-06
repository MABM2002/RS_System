# PDF Reporting Engine — AI Integration Guide

This document provides instructions for an AI assistant to integrate and use the `Reports.PdfEngine` library within the `RS_System` project.

## 🏗 Architecture
The library uses a 3-stage pipeline to generate PDFs:
1.  **Template Resolution**: `ITemplateResolver` (default: `FileTemplateResolver`) loads `.html` files from disk.
2.  **HTML Rendering**: `IHtmlRenderer` (default: `ScribanHtmlRenderer`) binds data to the template using **Scriban**.
3.  **PDF Conversion**: `PlaywrightPdfEngine` uses a headless **Chromium** browser to render the final HTML to PDF bytes.

## 🚀 Setup & Registration

Ensure the service is registered in `Program.cs`:

```csharp
using Reports.PdfEngine.DependencyInjection;

// Option A: Bind from appsettings.json (Section: "PdfEngine")
builder.Services.AddPdfReporting(builder.Configuration);

// Option B: Inline configuration
builder.Services.AddPdfReporting(options => {
    options.TemplatesPath = "Templates/Reports"; // Path relative to AppContext.BaseDirectory
    options.PaperFormat = "A4";
    options.Landscape = false;
});
```

### Required Configuration (`appsettings.json`)
```json
{
  "PdfEngine": {
    "TemplatesPath": "Templates/Reports",
    "TemplateExtension": ".html",
    "PaperFormat": "A4",
    "PrintBackground": true,
    "MarginTop": "10mm",
    "MarginBottom": "10mm",
    "MarginLeft": "10mm",
    "MarginRight": "10mm",
    "BrowserTimeoutMs": 30000,
    "EnableTemplateCache": true
  }
}
```

## 🛠 Usage in Controllers/Services

Inject `IReportGenerator` and call `GenerateAsync<T>`.

```csharp
using Reports.PdfEngine.Abstractions;

public class ReportController : Controller
{
    private readonly IReportGenerator _reportGenerator;

    public ReportController(IReportGenerator reportGenerator)
    {
        _reportGenerator = reportGenerator;
    }

    public async Task<IActionResult> Download(int id)
    {
        // 1. Get your DTO/POCO data
        var data = await _service.GetReportDataAsync(id);

        // 2. Generate the PDF
        // templateName: "Invoice" -> looks for "Templates/Reports/Invoice.html"
        byte[] pdfBytes = await _reportGenerator.GenerateAsync("Invoice", data);

        // 3. Return as file
        return File(pdfBytes, "application/pdf", $"Invoice_{id}.pdf");
    }
}
```

## 📝 Templating (Scriban)

Templates are standard HTML + Scriban directives. The property names of your model are preserved exactly (PascalCase).

**Example Template (`Templates/Reports/Invoice.html`):**
```html
<h1>Invoice #{{ Id }}</h1>
<p>Customer: {{ CustomerName }}</p>

<table>
    <thead>
        <tr><th>Product</th><th>Price</th></tr>
    </thead>
    <tbody>
        {{ for item in Items }}
        <tr>
            <td>{{ item.Name }}</td>
            <td>{{ item.Price | math.format "N2" }}</td>
        </tr>
        {{ end }}
    </tbody>
</table>

<p>Total: <strong>{{ TotalAmount | math.format "C2" }}</strong></p>
```

### Scriban Cheatsheet for this Engine:
- **Properties**: Access as `{{ PropertyName }}`.
- **Loops**: `{{ for item in List }}...{{ end }}`.
- **Conditions**: `{{ if IsActive }}...{{ else }}...{{ end }}`.
- **Formatting**: Use filters like `{{ Value | math.format "N2" }}` or `{{ Date | date.to_string "%d/%m/%Y" }}`.

## 🐳 Docker / Linux Environment
- The project is configured to run in **Linux Docker containers**.
- The `Dockerfile` must include Chromium system dependencies (libnss3, libatk, etc.).
- Playwright installs browsers to `/root/.cache/ms-playwright` during build.
- Native rendering requires `--no-sandbox` as configured in `PlaywrightPdfEngine.cs`.

## ⚠️ Important Constraints
1.  **Memory Management**: `PlaywrightPdfEngine` is a singleton and implements `IAsyncDisposable`. Do NOT dispose it manually; the DI container handles it at application shutdown.
2.  **Thread Safety**: The engine uses `SemaphoreSlim` for browser initialization and creates isolated contexts/pages per request. It is safe for concurrent use in ASP.NET Core.
3.  **Paths**: `TemplatesPath` defaults to a relative path. If running in production, ensure the directory exists in the published folder.
4.  **Model Type**: The generator requires the model to be a `class` (reference type). Avoid using primitive types directly as the top-level model.
