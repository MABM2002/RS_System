using Microsoft.Extensions.Logging.Abstractions;
using Reports.PdfEngine.Exceptions;
using Reports.PdfEngine.Rendering;

namespace Reports.PdfEngine.Tests;

public class ScribanHtmlRendererTests
{
    private readonly ScribanHtmlRenderer _renderer = new(NullLogger<ScribanHtmlRenderer>.Instance);

    [Fact]
    public async Task RenderAsync_SimpleModel_ProducesExpectedHtml()
    {
        // Arrange
        var template = "<h1>{{ Title }}</h1><p>{{ Description }}</p>";
        var model = new { Title = "Reporte Mensual", Description = "Datos del mes de enero" };

        // Act
        var result = await _renderer.RenderAsync(template, model);

        // Assert
        Assert.Contains("<h1>Reporte Mensual</h1>", result);
        Assert.Contains("<p>Datos del mes de enero</p>", result);
    }

    [Fact]
    public async Task RenderAsync_ModelWithCollection_IteratesCorrectly()
    {
        // Arrange
        var template = @"
<ul>
{{ for item in Items }}
<li>{{ item.Name }} - {{ item.Amount }}</li>
{{ end }}
</ul>";
        var model = new
        {
            Items = new[]
            {
                new { Name = "Diezmo", Amount = 500.00m },
                new { Name = "Ofrenda", Amount = 200.50m }
            }
        };

        // Act
        var result = await _renderer.RenderAsync(template, model);

        // Assert
        Assert.Contains("<li>Diezmo - 500.00</li>", result);
        Assert.Contains("<li>Ofrenda - 200.50</li>", result);
    }

    [Fact]
    public async Task RenderAsync_ConditionalBlock_HandlesCondition()
    {
        // Arrange
        var template = @"{{ if ShowFooter }}<footer>{{ FooterText }}</footer>{{ end }}";
        var model = new { ShowFooter = true, FooterText = "Pie de página" };

        // Act
        var result = await _renderer.RenderAsync(template, model);

        // Assert
        Assert.Contains("<footer>Pie de página</footer>", result);
    }

    [Fact]
    public async Task RenderAsync_ConditionalFalse_OmitsBlock()
    {
        // Arrange
        var template = @"{{ if ShowFooter }}<footer>Visible</footer>{{ end }}";
        var model = new { ShowFooter = false };

        // Act
        var result = await _renderer.RenderAsync(template, model);

        // Assert
        Assert.DoesNotContain("<footer>", result);
    }

    [Fact]
    public async Task RenderAsync_InvalidTemplate_ThrowsPdfRenderException()
    {
        // Arrange — unclosed block
        var template = "{{ for item in Items }}{{ item.Name }}";
        var model = new { Items = new[] { new { Name = "Test" } } };

        // Act & Assert
        await Assert.ThrowsAsync<PdfRenderException>(
            () => _renderer.RenderAsync(template, model));
    }

    [Fact]
    public async Task RenderAsync_NullModel_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _renderer.RenderAsync("<p>Test</p>", (object)null!));
    }

    [Fact]
    public async Task RenderAsync_EmptyTemplate_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _renderer.RenderAsync("", new { }));
    }

    [Fact]
    public async Task RenderAsync_NestedModel_AccessesNestedProperties()
    {
        // Arrange
        var template = "<p>{{ Church.Name }} - {{ Church.Pastor }}</p>";
        var model = new
        {
            Church = new { Name = "Iglesia Central", Pastor = "Juan Pérez" }
        };

        // Act
        var result = await _renderer.RenderAsync(template, model);

        // Assert
        Assert.Contains("<p>Iglesia Central - Juan Pérez</p>", result);
    }

    [Fact]
    public async Task RenderAsync_FormattedNumbers_PreservesFormatting()
    {
        // Arrange
        var template = @"<td>{{ Amount | math.format ""N2"" }}</td>";
        var model = new { Amount = 1234.5 };

        // Act
        var result = await _renderer.RenderAsync(template, model);

        // Assert
        Assert.Contains("1,234.50", result);
    }
}
