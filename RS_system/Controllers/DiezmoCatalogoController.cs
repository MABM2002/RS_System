using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Filters;
using Rs_system.Models;
using Rs_system.Models.ViewModels.Catalogos;
using System.Security.Claims;

namespace Rs_system.Controllers;

[Authorize]
[Permission("Diezmo.Index")] // Requiere permisos base del módulo
public class DiezmoCatalogoController : Controller
{
    private readonly ApplicationDbContext _context;

    public DiezmoCatalogoController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string UsuarioActual() => User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Sistema";

    // ─────────────────────────────────────────────────────────────────────────
    // Tipos de Salida
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> TiposSalida()
    {
        var lista = await _context.DiezmoTiposSalida
            .Where(x => !x.Eliminado)
            .OrderBy(x => x.Nombre)
            .ToListAsync();
        return View(lista);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTipoSalida(TipoSalidaViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(TiposSalida));
        }

        if (vm.Id == 0) // Crear
        {
            var nuevo = new DiezmoTipoSalida
            {
                Nombre          = vm.Nombre,
                Descripcion     = vm.Descripcion,
                EsEntregaPastor = vm.EsEntregaPastor,
                CreadoPor       = UsuarioActual(),
                CreadoEn        = DateTime.UtcNow
            };
            _context.DiezmoTiposSalida.Add(nuevo);
            TempData["SuccessMessage"] = "Tipo de salida creado.";
        }
        else // Editar
        {
            var dbItem = await _context.DiezmoTiposSalida.FindAsync(vm.Id);
            if (dbItem == null || dbItem.Eliminado) return NotFound();

            dbItem.Nombre          = vm.Nombre;
            dbItem.Descripcion     = vm.Descripcion;
            dbItem.EsEntregaPastor = vm.EsEntregaPastor;
            dbItem.ActualizadoEn   = DateTime.UtcNow;
            _context.Update(dbItem);

            TempData["SuccessMessage"] = "Tipo de salida actualizado.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(TiposSalida));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTipoSalida(long id)
    {
        var dbItem = await _context.DiezmoTiposSalida.FindAsync(id);
        if (dbItem == null) return NotFound();

        // Validación simple (si ya hay salidas con este tipo no borrar duro)
        var enUso = await _context.DiezmoSalidas.AnyAsync(s => s.TipoSalidaId == id && !s.Eliminado);
        if (enUso)
        {
            dbItem.Activo = false; // Desactivar en lugar de borrar
            dbItem.Eliminado = true;
            TempData["SuccessMessage"] = "Tipo de salida desactivado (Estaba en uso).";
        }
        else
        {
            dbItem.Eliminado = true;
            TempData["SuccessMessage"] = "Tipo de salida eliminado.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(TiposSalida));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Beneficiarios
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Beneficiarios()
    {
        var lista = await _context.DiezmoBeneficiarios
            .Where(x => !x.Eliminado)
            .OrderBy(x => x.Nombre)
            .ToListAsync();
        return View(lista);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarBeneficiario(BeneficiarioViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Beneficiarios));
        }

        if (vm.Id == 0)
        {
            var nuevo = new DiezmoBeneficiario
            {
                Nombre      = vm.Nombre,
                Descripcion = vm.Descripcion,
                CreadoPor   = UsuarioActual()
            };
            _context.DiezmoBeneficiarios.Add(nuevo);
            TempData["SuccessMessage"] = "Beneficiario creado.";
        }
        else
        {
            var dbItem = await _context.DiezmoBeneficiarios.FindAsync(vm.Id);
            if (dbItem == null || dbItem.Eliminado) return NotFound();

            dbItem.Nombre         = vm.Nombre;
            dbItem.Descripcion    = vm.Descripcion;
            dbItem.ActualizadoPor = UsuarioActual();
            dbItem.ActualizadoEn  = DateTime.UtcNow;
            _context.Update(dbItem);
            
            TempData["SuccessMessage"] = "Beneficiario actualizado.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Beneficiarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarBeneficiario(long id)
    {
        var dbItem = await _context.DiezmoBeneficiarios.FindAsync(id);
        if (dbItem == null) return NotFound();

        var enUso = await _context.DiezmoSalidas.AnyAsync(s => s.BeneficiarioId == id && !s.Eliminado);
        if (enUso)
        {
            dbItem.Activo = false;
            dbItem.Eliminado = true;
            TempData["SuccessMessage"] = "Beneficiario desactivado (estaba en uso).";
        }
        else
        {
            dbItem.Eliminado = true;
            TempData["SuccessMessage"] = "Beneficiario eliminado.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Beneficiarios));
    }
}
