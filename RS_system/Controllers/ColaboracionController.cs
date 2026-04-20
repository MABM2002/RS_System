using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models.ViewModels;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class ColaboracionController : Controller
{
    private readonly IColaboracionService _colaboracionService;
    private readonly IMiembroService _miembroService;
    
    public ColaboracionController(
        IColaboracionService colaboracionService,
        IMiembroService miembroService)
    {
        _colaboracionService = colaboracionService;
        _miembroService = miembroService;
    }
    
    // GET: Colaboracion
    public async Task<IActionResult> Index()
    {
        try
        {
            var colaboracionHeads = await _colaboracionService.GetColaboracionHeadsRecientesAsync();
            return View(colaboracionHeads);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar jornadas de colaboración: {ex.Message}";
            return View(new List<ColaboracionHeadIndexViewModel>());
        }
    }
    
    // GET: Colaboracion/Create
    public async Task<IActionResult> Create(int jornada)
    {
        try
        {
            var viewModel = new RegistrarColaboracionViewModel
            {
                MesInicial = DateTime.Now.Month,
                AnioInicial = DateTime.Now.Year,
                MesFinal = DateTime.Now.Month,
                AnioFinal = DateTime.Now.Year,
                MontoTotal = 0,
                TiposDisponibles = await _colaboracionService.GetTiposActivosAsync(),
                IdJornada = jornada
            };
            
            await CargarMiembrosAsync();
            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar formulario: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
    
    // POST: Colaboracion/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrarColaboracionViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var registradoPor = User.Identity?.Name ?? "Sistema";
                await _colaboracionService.RegistrarColaboracionAsync(model, registradoPor);
                
                TempData["Success"] = "Colaboración registrada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al registrar: {ex.Message}");
            }
        }
        
        // Recargar datos para la vista
        model.TiposDisponibles = await _colaboracionService.GetTiposActivosAsync();
        await CargarMiembrosAsync();
        return View(model);
    }
    
    // GET: Colaboracion/Details/5 (individual collaboration)
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var colaboracion = await _colaboracionService.GetColaboracionByIdAsync(id);
            if (colaboracion == null)
            {
                TempData["Error"] = "Colaboración no encontrada";
                return RedirectToAction(nameof(Index));
            }

            return View(colaboracion);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar detalle: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Colaboracion/Jornada/5 (colaboracion head details)
    public async Task<IActionResult> Jornada(long id)
    {
        try
        {
            var jornada = await _colaboracionService.GetColaboracionHeadByIdAsync(id);
            if (jornada == null)
            {
                TempData["Error"] = "Jornada no encontrada";
                return RedirectToAction(nameof(Index));
            }

            return View(jornada);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cargar jornada: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Colaboracion/CierreDiario/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CierreDiario(long id)
    {
        try
        {
            var cerradoPor = User.Identity?.Name ?? "Sistema";
            var resultado = await _colaboracionService.RealizarCierreDiarioAsync(id, cerradoPor);

            if (resultado.Success)
            {
                TempData["Success"] = resultado.Message;
            }
            else
            {
                TempData["Error"] = resultado.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al realizar cierre diario: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Colaboracion/Reportes
    public IActionResult Reportes()
    {
        ViewBag.FechaInicio = DateTime.Now.Date;
        ViewBag.FechaFin = DateTime.Now.Date;
        return View();
    }
    
    // POST: Colaboracion/GenerarReporte
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarReporte(DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            // Ajustar fecha fin para incluir todo el día
            var fechaFinAjustada = fechaFin.Date.AddDays(1).AddSeconds(-1);
            
            var reporte = await _colaboracionService.GenerarReportePorFechasAsync(
                fechaInicio.Date, 
                fechaFinAjustada);
            
            return View("Reporte", reporte);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar reporte: {ex.Message}";
            return RedirectToAction(nameof(Reportes));
        }
    }
    
    // GET: Colaboracion/EstadoCuenta/5
    public async Task<IActionResult> EstadoCuenta(long id)
    {
        try
        {
            var estado = await _colaboracionService.GenerarEstadoCuentaAsync(id);
            return View(estado);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar estado de cuenta: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Colaboracion/CrearJornada
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearJornada([FromBody] CrearJornadaRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request.Fecha == default)
            {
                return Json(new { success = false, message = "Fecha inválida" });
            }

            var creadoPor = User.Identity?.Name ?? "Sistema";
            var head = await _colaboracionService.GetOrCreateColaboracionHeadForDateAsync(request.Fecha, creadoPor);

            if (head == null)
            {
                return Json(new { success = false, message = "Error al crear la jornada" });
            }

            return Json(new { 
                success = true, 
                message = $"Jornada creada exitosamente para la fecha {request.Fecha:dd/MM/yyyy}",
                jornadaId = head.Id
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al crear jornada: {ex.Message}" });
        }
    }
    
    // GET: Colaboracion/BuscarMiembros?termino=juan
    [HttpGet]
    public async Task<IActionResult> BuscarMiembros(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
        {
            return Json(new List<object>());
        }
        
        try
        {
            var miembros = await _miembroService.GetAllAsync();
            
            var resultados = miembros
                .Where(m => 
                    m.Nombres.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                    m.Apellidos.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                    $"{m.Nombres} {m.Apellidos}".Contains(termino, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(m => new
                {
                    id = m.Id,
                    text = $"{m.Nombres} {m.Apellidos}",
                    telefono = m.Telefono
                })
                .ToList();
            
            return Json(resultados);
        }
        catch (Exception ex)
        {
            return Json(new List<object>());
        }
    }
    
    // GET: Colaboracion/ObtenerUltimosPagos?miembroId=5
    [HttpGet]
    public async Task<IActionResult> ObtenerUltimosPagos(long miembroId)
    {
        try
        {
            var ultimosPagos = await _colaboracionService.GetUltimosPagosPorMiembroAsync(miembroId);
            return Json(ultimosPagos);
        }
        catch (Exception ex)
        {
            return Json(new List<object>());
        }
    }
    
    // Helper methods
    private async Task CargarMiembrosAsync()
    {
        var miembros = await _miembroService.GetAllAsync();
        ViewBag.Miembros = new SelectList(
            miembros.Select(m => new
            {
                Id = m.Id,
                NombreCompleto = $"{m.Nombres} {m.Apellidos}"
            }),
            "Id",
            "NombreCompleto"
        );
    }
}
