using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class EstadosController : Controller
{
    private readonly IEstadoArticuloService _service;

    public EstadosController(IEstadoArticuloService service)
    {
        _service = service;
    }

    // GET: Estados
    public async Task<IActionResult> Index()
    {
        var list = await _service.GetAllAsync();
        return View(list);
    }

    // GET: Estados/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Estados/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,Color,Activo")] EstadoArticulo estado)
    {
        if (string.IsNullOrWhiteSpace(estado.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await _service.ExistsAsync(estado.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe un estado con ese nombre.");
                return View(estado);
            }

            estado.CreadoPor = User.Identity?.Name ?? "Sistema";
            var result = await _service.CreateAsync(estado);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Estado creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "Ocurrió un error al guardar los datos.");
        }
        return View(estado);
    }

    // GET: Estados/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var estado = await _service.GetByIdAsync(id.Value);
        if (estado == null) return NotFound();

        return View(estado);
    }

    // POST: Estados/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Color,Activo")] EstadoArticulo estado)
    {
        if (id != estado.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(estado.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await _service.ExistsAsync(estado.Nombre, id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otro estado con ese nombre.");
                return View(estado);
            }

            var result = await _service.UpdateAsync(estado);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Estado actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "No se pudo actualizar el estado o no fue encontrado.");
        }

        return View(estado);
    }

    // POST: Estados/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Estado eliminado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo eliminar el estado.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}
