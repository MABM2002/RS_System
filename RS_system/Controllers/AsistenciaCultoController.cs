using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using Rs_system.Models.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Rs_system.Controllers;

[Authorize]
public class AsistenciaCultoController : Controller
{
    private readonly ApplicationDbContext _context;

    public AsistenciaCultoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: AsistenciaCulto
    public async Task<IActionResult> Index(AsistenciaCultoFiltroViewModel filtro)
    {
        var query = _context.AsistenciasCulto.AsQueryable();
        
        // Aplicar filtros
        if (filtro.FechaDesde.HasValue)
        {
            query = query.Where(a => a.FechaHoraInicio >= filtro.FechaDesde.Value.Date);
        }
        
        if (filtro.FechaHasta.HasValue)
        {
            var fechaHasta = filtro.FechaHasta.Value.Date.AddDays(1).AddSeconds(-1);
            query = query.Where(a => a.FechaHoraInicio <= fechaHasta);
        }
        
        if (filtro.TipoCulto.HasValue)
        {
            query = query.Where(a => a.TipoCulto == filtro.TipoCulto.Value);
        }
        
        if (filtro.TipoConteo.HasValue)
        {
            query = query.Where(a => a.TipoConteo == filtro.TipoConteo.Value);
        }
        
        // Ordenar por fecha descendente (más reciente primero)
        query = query.OrderByDescending(a => a.FechaHoraInicio);
        
        var resultados = await query.ToListAsync();
        
        filtro.Resultados = resultados;
        
        // Pasar tipos de culto y conteo para dropdowns
        ViewBag.TiposCulto = Enum.GetValues(typeof(TipoCulto)).Cast<TipoCulto>().ToList();
        ViewBag.TiposConteo = Enum.GetValues(typeof(TipoConteo)).Cast<TipoConteo>().ToList();
        
        return View(filtro);
    }

    // GET: AsistenciaCulto/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var asistenciaCulto = await _context.AsistenciasCulto
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (asistenciaCulto == null)
        {
            return NotFound();
        }

        return View(asistenciaCulto);
    }

    // GET: AsistenciaCulto/Create
    public IActionResult Create()
    {
        var model = new AsistenciaCultoViewModel
        {
            FechaHoraInicio = DateTime.Now
        };
        
        ViewBag.TiposCulto = Enum.GetValues(typeof(TipoCulto)).Cast<TipoCulto>().ToList();
        ViewBag.TiposConteo = Enum.GetValues(typeof(TipoConteo)).Cast<TipoConteo>().ToList();
        
        return View(model);
    }

    // POST: AsistenciaCulto/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AsistenciaCultoViewModel model)
    {
        ViewBag.TiposCulto = Enum.GetValues(typeof(TipoCulto)).Cast<TipoCulto>().ToList();
        ViewBag.TiposConteo = Enum.GetValues(typeof(TipoConteo)).Cast<TipoConteo>().ToList();
        
        if (ModelState.IsValid)
        {
            var asistenciaCulto = new AsistenciaCulto
            {
                FechaHoraInicio = model.FechaHoraInicio,
                TipoCulto = model.TipoCulto,
                TipoConteo = model.TipoConteo,
                HermanasMisioneras = model.HermanasMisioneras,
                HermanosFraternidad = model.HermanosFraternidad,
                EmbajadoresCristo = model.EmbajadoresCristo,
                Ninos = model.Ninos,
                Visitas = model.Visitas,
                Amigos = model.Amigos,
                AdultosGeneral = model.AdultosGeneral,
                TotalManual = model.TotalManual,
                Observaciones = model.Observaciones,
                CreadoPor = User.Identity?.Name,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };
            
            _context.Add(asistenciaCulto);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        return View(model);
    }

    // GET: AsistenciaCulto/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var asistenciaCulto = await _context.AsistenciasCulto.FindAsync(id);
        if (asistenciaCulto == null)
        {
            return NotFound();
        }
        
        var model = new AsistenciaCultoViewModel
        {
            Id = asistenciaCulto.Id,
            FechaHoraInicio = asistenciaCulto.FechaHoraInicio,
            TipoCulto = asistenciaCulto.TipoCulto,
            TipoConteo = asistenciaCulto.TipoConteo,
            HermanasMisioneras = asistenciaCulto.HermanasMisioneras,
            HermanosFraternidad = asistenciaCulto.HermanosFraternidad,
            EmbajadoresCristo = asistenciaCulto.EmbajadoresCristo,
            Ninos = asistenciaCulto.Ninos,
            Visitas = asistenciaCulto.Visitas,
            Amigos = asistenciaCulto.Amigos,
            AdultosGeneral = asistenciaCulto.AdultosGeneral,
            TotalManual = asistenciaCulto.TotalManual,
            Observaciones = asistenciaCulto.Observaciones
        };
        
        ViewBag.TiposCulto = Enum.GetValues(typeof(TipoCulto)).Cast<TipoCulto>().ToList();
        ViewBag.TiposConteo = Enum.GetValues(typeof(TipoConteo)).Cast<TipoConteo>().ToList();
        
        return View(model);
    }

    // POST: AsistenciaCulto/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, AsistenciaCultoViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }
        
        ViewBag.TiposCulto = Enum.GetValues(typeof(TipoCulto)).Cast<TipoCulto>().ToList();
        ViewBag.TiposConteo = Enum.GetValues(typeof(TipoConteo)).Cast<TipoConteo>().ToList();
        
        if (ModelState.IsValid)
        {
            var asistenciaCulto = await _context.AsistenciasCulto.FindAsync(id);
            if (asistenciaCulto == null)
            {
                return NotFound();
            }
            
            asistenciaCulto.FechaHoraInicio = model.FechaHoraInicio;
            asistenciaCulto.TipoCulto = model.TipoCulto;
            asistenciaCulto.TipoConteo = model.TipoConteo;
            asistenciaCulto.HermanasMisioneras = model.HermanasMisioneras;
            asistenciaCulto.HermanosFraternidad = model.HermanosFraternidad;
            asistenciaCulto.EmbajadoresCristo = model.EmbajadoresCristo;
            asistenciaCulto.Ninos = model.Ninos;
            asistenciaCulto.Visitas = model.Visitas;
            asistenciaCulto.Amigos = model.Amigos;
            asistenciaCulto.AdultosGeneral = model.AdultosGeneral;
            asistenciaCulto.TotalManual = model.TotalManual;
            asistenciaCulto.Observaciones = model.Observaciones;
            asistenciaCulto.ActualizadoEn = DateTime.UtcNow;
            
            try
            {
                _context.Update(asistenciaCulto);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AsistenciaCultoExists(asistenciaCulto.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        return View(model);
    }

    // GET: AsistenciaCulto/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var asistenciaCulto = await _context.AsistenciasCulto
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (asistenciaCulto == null)
        {
            return NotFound();
        }

        return View(asistenciaCulto);
    }

    // POST: AsistenciaCulto/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var asistenciaCulto = await _context.AsistenciasCulto.FindAsync(id);
        if (asistenciaCulto != null)
        {
            _context.AsistenciasCulto.Remove(asistenciaCulto);
        }
        
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AsistenciaCultoExists(long id)
    {
        return _context.AsistenciasCulto.Any(e => e.Id == id);
    }
}