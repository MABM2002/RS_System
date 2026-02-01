using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class CategoriasController : Controller
{
    private readonly ICategoriaService _service;

    public CategoriasController(ICategoriaService service)
    {
        _service = service;
    }

    // GET: Categorias
    public async Task<IActionResult> Index()
    {
        var list = await _service.GetAllAsync();
        return View(list);
    }

    // GET: Categorias/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Categorias/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,Activo")] Categoria categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await _service.ExistsAsync(categoria.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría con ese nombre.");
                return View(categoria);
            }

            categoria.CreadoPor = User.Identity?.Name ?? "Sistema";
            var result = await _service.CreateAsync(categoria);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Categoría creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "Ocurrió un error al guardar los datos.");
        }
        return View(categoria);
    }

    // GET: Categorias/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var categoria = await _service.GetByIdAsync(id.Value);
        if (categoria == null) return NotFound();

        return View(categoria);
    }

    // POST: Categorias/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Activo")] Categoria categoria)
    {
        if (id != categoria.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            if (await _service.ExistsAsync(categoria.Nombre, id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otra categoría con ese nombre.");
                return View(categoria);
            }

            var result = await _service.UpdateAsync(categoria);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Categoría actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", "No se pudo actualizar la categoría o no fue encontrada.");
        }

        return View(categoria);
    }

    // POST: Categorias/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Categoría eliminada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo eliminar la categoría.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}
