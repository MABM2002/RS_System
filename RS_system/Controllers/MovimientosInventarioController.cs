using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class MovimientosInventarioController : Controller
{
    private readonly IMovimientoService _movimientoService;
    private readonly IArticuloService _articuloService;
    private readonly IUbicacionService _ubicacionService;
    private readonly IEstadoArticuloService _estadoService;
    private readonly IPrestamoService _prestamoService;

    public MovimientosInventarioController(
        IMovimientoService movimientoService,
        IArticuloService articuloService,
        IUbicacionService ubicacionService,
        IEstadoArticuloService estadoService,
        IPrestamoService prestamoService)
    {
        _movimientoService = movimientoService;
        _articuloService = articuloService;
        _ubicacionService = ubicacionService;
        _estadoService = estadoService;
        _prestamoService = prestamoService;
    }

    // GET: MovimientosInventario
    public async Task<IActionResult> Index()
    {
        var historial = await _movimientoService.GetHistorialGeneralAsync(50); // Limit 50 for performance
        return View(historial);
    }

    // GET: MovimientosInventario/Create
    // This is the "Wizard" or "Action Selector"
    public async Task<IActionResult> Create(int? articuloId)
    {
        if (articuloId.HasValue)
        {
            var articulo = await _articuloService.GetByIdAsync(articuloId.Value);
            if (articulo == null) return NotFound();
            
            ViewBag.ArticuloId = articulo.Id;
            ViewBag.ArticuloNombre = $"{articulo.Codigo} - {articulo.Nombre}";
            ViewBag.UbicacionActual = articulo.UbicacionNombre;
            ViewBag.EstadoActual = articulo.EstadoNombre;
            ViewBag.TipoControl = articulo.TipoControl; // "UNITARIO" or "LOTE"
            ViewBag.CantidadGlobal = articulo.CantidadGlobal; // For LOTE validation?
        }

        ViewBag.Articulos = new SelectList((await _articuloService.GetAllAsync()).Select(x => new { x.Id, Nombre = $"{x.Codigo} - {x.Nombre}" }), "Id", "Nombre", articuloId);
        ViewBag.Ubicaciones = new SelectList(await _ubicacionService.GetAllAsync(), "Id", "Nombre");
        ViewBag.Estados = new SelectList(await _estadoService.GetAllAsync(), "Id", "Nombre");

        return View();
    }

    // POST: MovimientosInventario/RegistrarTraslado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarTraslado(int articuloId, int nuevaUbicacionId, string observacion, int cantidad = 1)
    {
        var usuario = User.Identity?.Name ?? "Sistema";
        // Use the new Quantity-Aware method
        var result = await _movimientoService.RegistrarTrasladoCantidadAsync(articuloId, nuevaUbicacionId, cantidad, observacion, usuario);

        if (result)
        {
            TempData["SuccessMessage"] = "Traslado registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Error al registrar el traslado. Verifique stock o campos.";
        return RedirectToAction(nameof(Create), new { articuloId });
    }

    // POST: MovimientosInventario/RegistrarBaja
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarBaja(int articuloId, string motivo, int cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "Debe especificar un motivo para la baja.";
            return RedirectToAction(nameof(Create), new { articuloId });
        }

        var usuario = User.Identity?.Name ?? "Sistema";
        var result = await _movimientoService.RegistrarBajaCantidadAsync(articuloId, cantidad, motivo, usuario);

        if (result)
        {
            TempData["SuccessMessage"] = "Baja registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Error al registrar la baja.";
        return RedirectToAction(nameof(Create), new { articuloId });
    }

    // POST: MovimientosInventario/RegistrarCambioEstado
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarCambioEstado(int articuloId, int nuevoEstadoId, string observacion)
    {
        var usuario = User.Identity?.Name ?? "Sistema";
        var result = await _movimientoService.RegistrarCambioEstadoAsync(articuloId, nuevoEstadoId, observacion, usuario);

        if (result)
        {
            TempData["SuccessMessage"] = "Cambio de estado registrado correctamento.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Error al registrar el cambio de estado. Verifique que el estado sea diferente al actual.";
        return RedirectToAction(nameof(Create), new { articuloId });
    }

    // POST: MovimientosInventario/RegistrarPrestamo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPrestamo(int articuloId, int cantidad, string personaNombre, string personaIdentificacion, DateTime? fechaDevolucionEstimada, string observacion)
    {
        if (string.IsNullOrWhiteSpace(personaNombre))
        {
            TempData["ErrorMessage"] = "Debe especificar el nombre de la persona a quien se presta el artículo.";
            return RedirectToAction(nameof(Create), new { articuloId });
        }

        var usuario = User.Identity?.Name ?? "Sistema";
        var result = await _prestamoService.RegistrarPrestamoAsync(articuloId, cantidad, personaNombre, personaIdentificacion, fechaDevolucionEstimada, observacion, usuario);

        if (result)
        {
            TempData["SuccessMessage"] = "Préstamo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Error al registrar el préstamo. Verifique stock disponible.";
        return RedirectToAction(nameof(Create), new { articuloId });
    }

    // GET: MovimientosInventario/PrestamosActivos
    public async Task<IActionResult> PrestamosActivos()
    {
        var prestamosActivos = await _prestamoService.GetPrestamosActivosAsync();
        return View(prestamosActivos);
    }

    // POST: MovimientosInventario/RegistrarDevolucion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarDevolucion(long prestamoId, string observacion)
    {
        var usuario = User.Identity?.Name ?? "Sistema";
        var result = await _prestamoService.RegistrarDevolucionAsync(prestamoId, observacion, usuario);

        if (result)
        {
            TempData["SuccessMessage"] = "Devolución registrada correctamente.";
            return RedirectToAction(nameof(PrestamosActivos));
        }

        TempData["ErrorMessage"] = "Error al registrar la devolución.";
        return RedirectToAction(nameof(PrestamosActivos));
    }

    // POST: MovimientosInventario/RegistrarEntrada
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarEntrada(int articuloId, int cantidad, string observacion)
    {
        var usuario = User.Identity?.Name ?? "Sistema";
        var result = await _movimientoService.RegistrarEntradaCantidadAsync(articuloId, cantidad, observacion, usuario);

        if (result)
        {
            TempData["SuccessMessage"] = "Entrada de inventario registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Error al registrar la entrada de inventario.";
        return RedirectToAction(nameof(Create), new { articuloId });
    }
}
