using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Reports.PdfEngine.Configuration;
using Reports.PdfEngine.Exceptions;
using System.IO;

namespace Reports.PdfEngine.Rendering;

/// <summary>
/// Converts fully rendered HTML into PDF bytes using Playwright's Chromium headless browser.
/// Implements <see cref="IAsyncDisposable"/> for deterministic cleanup of native browser resources.
/// Designed as a Singleton: lazily initializes one shared IBrowser instance, creates
/// a new IBrowserContext + IPage per PDF request for thread-safety.
/// </summary>
public sealed class PlaywrightPdfEngine : IAsyncDisposable
{
    private readonly PdfEngineOptions _options;
    private readonly ILogger<PlaywrightPdfEngine> _logger;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile bool _disposed;

    public PlaywrightPdfEngine(
        IOptions<PdfEngineOptions> options,
        ILogger<PlaywrightPdfEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Converts full HTML content to a PDF byte array.
    /// </summary>
    public async Task<byte[]> HtmlToPdfAsync(string htmlContent, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);

        var browser = await EnsureBrowserAsync().ConfigureAwait(false);

        // Each request gets its own context + page for full isolation.
        await using var context = await browser.NewContextAsync().ConfigureAwait(false);
        var page = await context.NewPageAsync().ConfigureAwait(false);

        try
        {
            await page.SetContentAsync(htmlContent, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = _options.BrowserTimeoutMs
            }).ConfigureAwait(false);

            var pdfBytes = await page.PdfAsync(new PagePdfOptions
            {
                Format = _options.PaperFormat,
                PrintBackground = _options.PrintBackground,
                Landscape = _options.Landscape,
                Margin = new Margin
                {
                    Top = _options.MarginTop,
                    Bottom = _options.MarginBottom,
                    Left = _options.MarginLeft,
                    Right = _options.MarginRight
                }
            }).ConfigureAwait(false);

            _logger.LogDebug("PDF generated ({Size} bytes)", pdfBytes.Length);
            return pdfBytes;
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright failed to generate PDF");
            throw new PdfRenderException("Browser-based PDF rendering failed.", ex);
        }
        finally
        {
            await page.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Thread-safe lazy initialization of the Playwright instance and shared browser.
    /// </summary>
    private async Task<IBrowser> EnsureBrowserAsync()
    {
        if (_browser is { IsConnected: true })
            return _browser;

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_browser is { IsConnected: true })
                return _browser;

            _logger.LogInformation("Initializing Playwright and launching Chromium...");

            _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            
            // Detect if running on Linux and use system Chromium (Fedora, etc.)
            // where Playwright's bundled Chromium may not work
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                }
            };

            // Use system Chromium on Linux (Fedora, etc.) where Playwright's bundled Chromium may not work
            if (OperatingSystem.IsLinux())
            {
                var systemChromiumPaths = new[]
                {
                    "/usr/bin/chromium",
                    "/usr/bin/chromium-browser",
                    "/usr/bin/google-chrome",
                    "/usr/bin/google-chrome-stable"
                };

                var chromiumPath = systemChromiumPaths.FirstOrDefault(File.Exists);
                if (!string.IsNullOrEmpty(chromiumPath))
                {
                    launchOptions.ExecutablePath = chromiumPath;
                    _logger.LogInformation("Using system Chromium at: {Path}", chromiumPath);
                }
                else
                {
                    _logger.LogWarning("No system Chromium found, falling back to Playwright's bundled Chromium");
                }
            }

            _browser = await _playwright.Chromium.LaunchAsync(launchOptions).ConfigureAwait(false);

            _logger.LogInformation("Chromium browser launched successfully");
            return _browser;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize Playwright/Chromium");
            throw new PdfRenderException("Could not start headless browser.", ex);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Disposing PlaywrightPdfEngine...");

        if (_browser is not null)
        {
            try { await _browser.CloseAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogWarning(ex, "Error closing browser"); }
        }

        _playwright?.Dispose();
        _initLock.Dispose();

        _logger.LogInformation("PlaywrightPdfEngine disposed");
    }
}
