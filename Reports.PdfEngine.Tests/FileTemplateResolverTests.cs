using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reports.PdfEngine.Configuration;
using Reports.PdfEngine.Exceptions;
using Reports.PdfEngine.Templates;

namespace Reports.PdfEngine.Tests;

public class FileTemplateResolverTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileTemplateResolver _resolver;

    public FileTemplateResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"pdf_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new PdfEngineOptions
        {
            TemplatesPath = _tempDir,
            TemplateExtension = ".html",
            EnableTemplateCache = true
        });

        _resolver = new FileTemplateResolver(options, NullLogger<FileTemplateResolver>.Instance);
    }

    [Fact]
    public async Task ResolveAsync_ExistingTemplate_ReturnsContent()
    {
        // Arrange
        var expected = "<h1>Test Template</h1>";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "test_report.html"), expected);

        // Act
        var result = await _resolver.ResolveAsync("test_report");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ResolveAsync_NonExistentTemplate_ThrowsTemplateNotFoundException()
    {
        await Assert.ThrowsAsync<TemplateNotFoundException>(
            () => _resolver.ResolveAsync("nonexistent"));
    }

    [Fact]
    public async Task ResolveAsync_CachedTemplate_ReturnsSameContent()
    {
        // Arrange
        var content = "<p>Cached</p>";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "cached.html"), content);

        // Act
        var first = await _resolver.ResolveAsync("cached");
        var second = await _resolver.ResolveAsync("cached");

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task ResolveAsync_EmptyTemplateName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _resolver.ResolveAsync(""));
    }

    [Fact]
    public async Task ResolveAsync_TemplateWithExtension_ResolvesCorrectly()
    {
        // Arrange
        var content = "<p>With Extension</p>";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "report.html"), content);

        // Act
        var result = await _resolver.ResolveAsync("report.html");

        // Assert
        Assert.Equal(content, result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
