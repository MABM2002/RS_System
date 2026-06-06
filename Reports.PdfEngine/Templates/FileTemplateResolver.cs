using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reports.PdfEngine.Abstractions;
using Reports.PdfEngine.Configuration;
using Reports.PdfEngine.Exceptions;

namespace Reports.PdfEngine.Templates;

/// <summary>
/// Resolves HTML templates from the local file system with optional in-memory caching.
/// </summary>
public sealed class FileTemplateResolver : ITemplateResolver
{
    private readonly PdfEngineOptions _options;
    private readonly ILogger<FileTemplateResolver> _logger;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public FileTemplateResolver(
        IOptions<PdfEngineOptions> options,
        ILogger<FileTemplateResolver> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(string templateName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        if (_options.EnableTemplateCache && _cache.TryGetValue(templateName, out var cached))
        {
            _logger.LogDebug("Template '{TemplateName}' served from cache", templateName);
            return cached;
        }

        var filePath = BuildTemplatePath(templateName);

        if (!File.Exists(filePath))
        {
            _logger.LogError("Template file not found: {FilePath}", filePath);
            throw new TemplateNotFoundException(templateName, filePath);
        }

        _logger.LogDebug("Loading template from disk: {FilePath}", filePath);
        var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);

        if (_options.EnableTemplateCache)
        {
            _cache.TryAdd(templateName, content);
        }

        return content;
    }

    private string BuildTemplatePath(string templateName)
    {
        var basePath = Path.IsPathRooted(_options.TemplatesPath)
            ? _options.TemplatesPath
            : Path.Combine(AppContext.BaseDirectory, _options.TemplatesPath);

        var fileName = templateName.EndsWith(_options.TemplateExtension, StringComparison.OrdinalIgnoreCase)
            ? templateName
            : templateName + _options.TemplateExtension;

        return Path.Combine(basePath, fileName);
    }
}
