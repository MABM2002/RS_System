using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class UbicacionesController : Controller
{
    private readonly IUbicacionService _service;

    public UbicacionesController(IUbicacionService service)
    {
        _service = service;
    }

    // GET: Ubicaciones
    public async Task<IActionResult> Index()
    {
        var list = await _service.GetAllAsync();
        return View(list);
    }

    // GET: Ubicaciones/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Ubicaciones/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,Responsable,Activo")] Ubicacion ubicacion)
    {
        if (string.IsNullOrWhiteSpace(ubicacion.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await _service.ExistsAsync(ubicacion.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una ubicación con ese nombre.");
                return View(ubicacion);
            }

            ubicacion.CreadoPor = User.Identity?.Name ?? "Sistema";
            var result = await _service.CreateAsync(ubicacion);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Ubicación creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "Ocurrió un error al guardar los datos.");
        }
        return View(ubicacion);
    }

    // GET: Ubicaciones/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var ubicacion = await _service.GetByIdAsync(id.Value);
        if (ubicacion == null) return NotFound();

        return View(ubicacion);
    }

    // POST: Ubicaciones/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Responsable,Activo")] Ubicacion ubicacion)
    {
        if (id != ubicacion.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(ubicacion.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await _service.ExistsAsync(ubicacion.Nombre, id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otra ubicación con ese nombre.");
                return View(ubicacion);
            }

            var result = await _service.UpdateAsync(ubicacion);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Ubicación actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "No se pudo actualizar la ubicación o no fue encontrada.");
        }

        return View(ubicacion);
    }

    // POST: Ubicaciones/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Ubicación eliminada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo eliminar la ubicación.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}
