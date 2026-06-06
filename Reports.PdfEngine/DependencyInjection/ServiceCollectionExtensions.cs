using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reports.PdfEngine.Abstractions;
using Reports.PdfEngine.Configuration;
using Reports.PdfEngine.Rendering;
using Reports.PdfEngine.Templates;

namespace Reports.PdfEngine.DependencyInjection;

/// <summary>
/// Extension methods for registering PDF reporting services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PDF reporting engine and all its dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="PdfEngineOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPdfReporting(
        this IServiceCollection services,
        Action<PdfEngineOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // Configuration
        services.Configure(configure);

        // Template resolution — Scoped (one per request scope)
        services.AddScoped<ITemplateResolver, FileTemplateResolver>();

        // HTML rendering — Scoped
        services.AddScoped<IHtmlRenderer, ScribanHtmlRenderer>();

        // Playwright browser engine — Singleton (reuses one Chromium instance)
        services.AddSingleton<PlaywrightPdfEngine>();

        // Report generator (orchestrator) — Scoped
        services.AddScoped<IReportGenerator, ReportGenerator>();

        return services;
    }

    /// <summary>
    /// Registers the PDF reporting engine using the "PdfEngine" configuration section.
    /// </summary>
    public static IServiceCollection AddPdfReporting(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var section = configuration.GetSection(PdfEngineOptions.SectionName);

        services.Configure<PdfEngineOptions>(section);
        services.AddScoped<ITemplateResolver, FileTemplateResolver>();
        services.AddScoped<IHtmlRenderer, ScribanHtmlRenderer>();
        services.AddSingleton<PlaywrightPdfEngine>();
        services.AddScoped<IReportGenerator, ReportGenerator>();

        return services;
    }
}
