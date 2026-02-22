using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Filters;
using Rs_system.Models.ViewModels;
using Rs_system.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Rs_system.Controllers;

[Authorize]
public class DiezmoController : Controller
{
    private readonly IDiezmoCierreService  _cierreService;
    private readonly IDiezmoReciboService  _reciboService;
    private readonly IMiembroService       _miembroService;

    public DiezmoController(
        IDiezmoCierreService  cierreService,
        IDiezmoReciboService  reciboService,
        IMiembroService       miembroService)
    {
        _cierreService  = cierreService;
        _reciboService  = reciboService;
        _miembroService = miembroService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: /Diezmo — Listado de cierres
    // ─────────────────────────────────────────────────────────────────────────
    [Permission("Diezmo.Index")]
    public async Task<IActionResult> Index(int? anio)
    {
        anio ??= DateTime.Today.Year;
        var cierres = await _cierreService.GetCierresAsync(anio);

        var vm = cierres.Select(c => new DiezmoCierreListViewModel
        {
            Id             = c.Id,
            Fecha          = c.Fecha,
            Cerrado        = c.Cerrado,
            TotalRecibido  = c.TotalRecibido,
            TotalNeto      = c.TotalNeto,
            TotalSalidas   = c.TotalSalidas,
            SaldoFinal     = c.SaldoFinal,
            NumeroDetalles = c.Detalles?.Count ?? 0,
            NumeroSalidas  = c.Salidas?.Count  ?? 0
        }).ToList();

        ViewBag.AnioActual = anio;
        ViewBag.Anios      = GetAniosSelectList();
        return View(vm);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: /Diezmo/Create
    // ─────────────────────────────────────────────────────────────────────────
    [Permission("Diezmo.Create")]
    public IActionResult Create()
        => View(new DiezmoCierreCreateViewModel());

    // POST: /Diezmo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.Create")]
    public async Task<IActionResult> Create(DiezmoCierreCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            var cierre = await _cierreService.CrearCierreAsync(
                vm.Fecha, vm.Observaciones, UsuarioActual());

            TempData["SuccessMessage"] = $"Cierre del {cierre.Fecha:dd/MM/yyyy} creado exitosamente.";
            return RedirectToAction(nameof(Detail), new { id = cierre.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Fecha", ex.Message);
            return View(vm);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: /Diezmo/Detail/{id} — Pantalla operativa
    // ─────────────────────────────────────────────────────────────────────────
    [Permission("Diezmo.Index")]
    public async Task<IActionResult> Detail(long id)
    {
        var cierre = await _cierreService.GetCierreByIdAsync(id);
        if (cierre == null || cierre.Eliminado) return NotFound();

        var tiposSalida   = await _cierreService.GetTiposSalidaActivosAsync();
        var beneficiarios = await _cierreService.GetBeneficiariosActivosAsync();
        var todosMiembros = await _miembroService.GetAllAsync();
        var miembrosSelect = todosMiembros
            .Where(m => m.Activo)
            .OrderBy(m => m.NombreCompleto)
            .Select(m => new SelectListItem(m.NombreCompleto, m.Id.ToString()))
            .ToList();

        var vm = new DiezmoCierreDetalleViewModel
        {
            Id            = cierre.Id,
            Fecha         = cierre.Fecha,
            Cerrado       = cierre.Cerrado,
            Observaciones = cierre.Observaciones,
            CerradoPor    = cierre.CerradoPor,
            FechaCierre   = cierre.FechaCierre,
            TotalRecibido = cierre.TotalRecibido,
            TotalCambio   = cierre.TotalCambio,
            TotalNeto     = cierre.TotalNeto,
            TotalSalidas  = cierre.TotalSalidas,
            SaldoFinal    = cierre.SaldoFinal,

            Detalles = cierre.Detalles.Select(d => new DiezmoDetalleRowViewModel
            {
                Id              = d.Id,
                MiembroId       = d.MiembroId,
                NombreMiembro   = d.Miembro?.Persona?.NombreCompleto ?? "—",
                MontoEntregado  = d.MontoEntregado,
                CambioEntregado = d.CambioEntregado,
                MontoNeto       = d.MontoNeto,
                Observaciones   = d.Observaciones,
                Fecha           = d.Fecha
            }).ToList(),

            Salidas = cierre.Salidas.Select(s => new DiezmoSalidaRowViewModel
            {
                Id                 = s.Id,
                TipoSalidaNombre   = s.TipoSalida?.Nombre ?? "—",
                BeneficiarioNombre = s.Beneficiario?.Nombre,
                Monto              = s.Monto,
                Concepto           = s.Concepto,
                NumeroRecibo       = s.NumeroRecibo,
                Fecha              = s.Fecha
            }).ToList(),

            MiembrosSelect = miembrosSelect,
            TiposSalidaSelect = tiposSalida.Select(t =>
                new SelectListItem(t.Nombre, t.Id.ToString())).ToList(),
            BeneficiariosSelect = beneficiarios.Select(b =>
                new SelectListItem(b.Nombre, b.Id.ToString())).ToList()
        };

        return View(vm);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: /Diezmo/AddDetalle
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.AddDetalle")]
    public async Task<IActionResult> AddDetalle(long cierreId, DiezmoDetalleFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest("Datos inválidos.");

            TempData["ErrorMessage"] = "Datos inválidos. Verifique el formulario.";
            return RedirectToAction(nameof(Detail), new { id = cierreId });
        }

        try
        {
            await _cierreService.AgregarDetalleAsync(cierreId, vm, UsuarioActual());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return await GetTotalesJsonAsync(cierreId);

            TempData["SuccessMessage"] = "Diezmo registrado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = cierreId });
    }

    // POST: /Diezmo/DeleteDetalle
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.AddDetalle")]
    public async Task<IActionResult> DeleteDetalle(long detalleId, long cierreId)
    {
        try
        {
            await _cierreService.EliminarDetalleAsync(detalleId, UsuarioActual());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return await GetTotalesJsonAsync(cierreId);

            TempData["SuccessMessage"] = "Detalle eliminado.";
        }
        catch (InvalidOperationException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = cierreId });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: /Diezmo/AddSalida
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.AddSalida")]
    public async Task<IActionResult> AddSalida(long cierreId, DiezmoSalidaFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest("Datos inválidos.");

            TempData["ErrorMessage"] = "Datos inválidos. Verifique el formulario.";
            return RedirectToAction(nameof(Detail), new { id = cierreId });
        }

        try
        {
            await _cierreService.AgregarSalidaAsync(cierreId, vm, UsuarioActual());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return await GetTotalesJsonAsync(cierreId);

            TempData["SuccessMessage"] = "Salida registrada correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = cierreId });
    }

    // POST: /Diezmo/DeleteSalida
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.AddSalida")]
    public async Task<IActionResult> DeleteSalida(long salidaId, long cierreId)
    {
        try
        {
            await _cierreService.EliminarSalidaAsync(salidaId, UsuarioActual());
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return await GetTotalesJsonAsync(cierreId);

            TempData["SuccessMessage"] = "Salida eliminada.";
        }
        catch (InvalidOperationException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(ex.Message);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = cierreId });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: /Diezmo/Close/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.Close")]
    public async Task<IActionResult> Close(long id)
    {
        try
        {
            await _cierreService.CerrarCierreAsync(id, UsuarioActual());
            TempData["SuccessMessage"] = "Cierre sellado exitosamente.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: /Diezmo/Reopen/{id}  — Solo Administrador
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("Diezmo.Reopen")]
    public async Task<IActionResult> Reopen(long id)
    {
        try
        {
            await _cierreService.ReabrirCierreAsync(id, UsuarioActual());
            TempData["SuccessMessage"] = "Cierre reabierto.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: /Diezmo/Recibo/{salidaId}
    // ─────────────────────────────────────────────────────────────────────────
    [Permission("Diezmo.Index")]
    public async Task<IActionResult> Recibo(long salidaId)
    {
        var numero = await _reciboService.GenerarNumeroReciboAsync(salidaId);
        var salida = await _reciboService.GetSalidaParaReciboAsync(salidaId);

        if (salida == null) return NotFound();

        ViewBag.NumeroRecibo = numero;
        ViewBag.Emisor       = UsuarioActual();
        return View(salida);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ─────────────────────────────────────────────────────────────────────────
    private string UsuarioActual()
        => User.FindFirst(ClaimTypes.Name)?.Value
           ?? User.Identity?.Name
           ?? "Sistema";

    private static List<SelectListItem> GetAniosSelectList()
    {
        var anioActual = DateTime.Today.Year;
        return Enumerable.Range(anioActual - 3, 5)
            .Select(a => new SelectListItem(a.ToString(), a.ToString()))
            .ToList();
    }

    private async Task<IActionResult> GetTotalesJsonAsync(long id)
    {
        var cierre = await _cierreService.GetCierreByIdAsync(id);
        if (cierre == null) return NotFound();

        return Json(new {
            totalRecibido = cierre.TotalRecibido,
            totalCambio   = cierre.TotalCambio,
            totalNeto     = cierre.TotalNeto,
            totalSalidas  = cierre.TotalSalidas,
            saldoFinal    = cierre.SaldoFinal
        });
    }
}
