using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Rs_system.Controllers;

[Authorize]
public class OfrendaController : Controller
{
    private readonly ApplicationDbContext _context;

    public OfrendaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Ofrenda
    public async Task<IActionResult> Index(int? mes, int? anio)
    {
        var currentDate = DateTime.Today;
        mes ??= currentDate.Month;
        anio ??= currentDate.Year;

        var registros = await _context.RegistrosCulto
            .Include(r => r.Ofrendas.Where(o => !o.Eliminado))
                .ThenInclude(o => o.Descuentos.Where(d => !d.Eliminado))
            .Where(r => !r.Eliminado && r.Fecha.Month == mes && r.Fecha.Year == anio)
            .OrderByDescending(r => r.Fecha)
            .ToListAsync();

        ViewBag.MesActual = mes;
        ViewBag.AnioActual = anio;
        ViewBag.Meses = GetMesesSelectList();
        ViewBag.Anios = GetAniosSelectList();

        return View(registros);
    }

    // GET: Ofrenda/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
            return NotFound();

        var registro = await _context.RegistrosCulto
            .Include(r => r.Ofrendas.Where(o => !o.Eliminado))
                .ThenInclude(o => o.Descuentos.Where(d => !d.Eliminado))
            .FirstOrDefaultAsync(r => r.Id == id && !r.Eliminado);

        if (registro == null)
            return NotFound();

        return View(registro);
    }

    // GET: Ofrenda/Create
    public IActionResult Create()
    {
        var viewModel = new RegistroCultoViewModel
        {
            Fecha = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(viewModel);
    }

    // POST: Ofrenda/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistroCultoViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    var registro = new RegistroCulto
                    {
                        Fecha = viewModel.Fecha,
                        Observaciones = viewModel.Observaciones,
                        CreadoPor = User.Identity?.Name ?? "Sistema",
                        CreadoEn = DateTime.UtcNow,
                        ActualizadoEn = DateTime.UtcNow
                    };

                    _context.RegistrosCulto.Add(registro);
                    await _context.SaveChangesAsync();

                    // Add offerings
                    foreach (var ofrendaVm in viewModel.Ofrendas)
                    {
                        var ofrenda = new Ofrenda
                        {
                            RegistroCultoId = registro.Id,
                            Monto = ofrendaVm.Monto,
                            Concepto = ofrendaVm.Concepto
                        };
                        _context.Ofrendas.Add(ofrenda);
                        await _context.SaveChangesAsync();

                        // Add deductions
                        foreach (var descuentoVm in ofrendaVm.Descuentos)
                        {
                            var descuento = new DescuentoOfrenda
                            {
                                OfrendaId = ofrenda.Id,
                                Monto = descuentoVm.Monto,
                                Concepto = descuentoVm.Concepto
                            };
                            _context.DescuentosOfrenda.Add(descuento);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                TempData["SuccessMessage"] = "Registro de ofrendas creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Error al guardar el registro. Intente nuevamente.");
            }
        }

        return View(viewModel);
    }

    // GET: Ofrenda/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
            return NotFound();

        var registro = await _context.RegistrosCulto
            .Include(r => r.Ofrendas.Where(o => !o.Eliminado))
                .ThenInclude(o => o.Descuentos.Where(d => !d.Eliminado))
            .FirstOrDefaultAsync(r => r.Id == id && !r.Eliminado);

        if (registro == null)
            return NotFound();

        var viewModel = new RegistroCultoViewModel
        {
            Id = registro.Id,
            Fecha = registro.Fecha,
            Observaciones = registro.Observaciones,
            Ofrendas = registro.Ofrendas.Select(o => new OfrendaItemViewModel
            {
                Id = o.Id,
                Monto = o.Monto,
                Concepto = o.Concepto,
                Descuentos = o.Descuentos.Select(d => new DescuentoItemViewModel
                {
                    Id = d.Id,
                    Monto = d.Monto,
                    Concepto = d.Concepto
                }).ToList()
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Ofrenda/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, RegistroCultoViewModel viewModel)
    {
        if (id != viewModel.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    var registro = await _context.RegistrosCulto
                        .Include(r => r.Ofrendas)
                            .ThenInclude(o => o.Descuentos)
                        .FirstOrDefaultAsync(r => r.Id == id && !r.Eliminado);

                    if (registro == null)
                        throw new InvalidOperationException("Registro no encontrado");

                    registro.Fecha = viewModel.Fecha;
                    registro.Observaciones = viewModel.Observaciones;
                    registro.ActualizadoEn = DateTime.UtcNow;

                    // Mark existing offerings as deleted
                    foreach (var ofrenda in registro.Ofrendas)
                    {
                        ofrenda.Eliminado = true;
                        foreach (var descuento in ofrenda.Descuentos)
                            descuento.Eliminado = true;
                    }

                    // Add new offerings
                    foreach (var ofrendaVm in viewModel.Ofrendas)
                    {
                        var ofrenda = new Ofrenda
                        {
                            RegistroCultoId = registro.Id,
                            Monto = ofrendaVm.Monto,
                            Concepto = ofrendaVm.Concepto
                        };
                        _context.Ofrendas.Add(ofrenda);
                        await _context.SaveChangesAsync();

                        foreach (var descuentoVm in ofrendaVm.Descuentos)
                        {
                            var descuento = new DescuentoOfrenda
                            {
                                OfrendaId = ofrenda.Id,
                                Monto = descuentoVm.Monto,
                                Concepto = descuentoVm.Concepto
                            };
                            _context.DescuentosOfrenda.Add(descuento);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                TempData["SuccessMessage"] = "Registro de ofrendas actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Error al actualizar el registro. Intente nuevamente.");
            }
        }

        return View(viewModel);
    }

    // POST: Ofrenda/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var registro = await _context.RegistrosCulto.FindAsync(id);
        if (registro == null)
            return NotFound();

        registro.Eliminado = true;
        registro.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Registro eliminado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    private List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetMesesSelectList()
    {
        var meses = new[]
        {
            new { Value = 1, Text = "Enero" },
            new { Value = 2, Text = "Febrero" },
            new { Value = 3, Text = "Marzo" },
            new { Value = 4, Text = "Abril" },
            new { Value = 5, Text = "Mayo" },
            new { Value = 6, Text = "Junio" },
            new { Value = 7, Text = "Julio" },
            new { Value = 8, Text = "Agosto" },
            new { Value = 9, Text = "Septiembre" },
            new { Value = 10, Text = "Octubre" },
            new { Value = 11, Text = "Noviembre" },
            new { Value = 12, Text = "Diciembre" }
        };

        return meses.Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = m.Value.ToString(),
            Text = m.Text
        }).ToList();
    }

    private List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetAniosSelectList()
    {
        var currentYear = DateTime.Today.Year;
        return Enumerable.Range(currentYear - 5, 10)
            .Select(y => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = y.ToString(),
                Text = y.ToString()
            }).ToList();
    }
}
