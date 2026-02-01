using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models.ViewModels;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class ArticulosController : Controller
{
    private readonly IArticuloService _service;

    public ArticulosController(IArticuloService service)
    {
        _service = service;
    }

    // GET: Articulos
    public async Task<IActionResult> Index(string? search, int? categoriaId, int? ubicacionId, int? estadoId)
    {
        // Load filter lists
        var categorias = await _service.GetCategoriasAsync();
        ViewBag.Categorias = new SelectList(categorias.Select(c => new { c.Id, c.Nombre }), "Id", "Nombre", categoriaId);

        var ubicaciones = await _service.GetUbicacionesAsync();
        ViewBag.Ubicaciones = new SelectList(ubicaciones.Select(u => new { u.Id, u.Nombre }), "Id", "Nombre", ubicacionId);
        
        // Custom Estado SelectList
        var estados = await _service.GetEstadosAsync();
        ViewBag.Estados = new SelectList(estados.Select(e => new { e.Id, e.Nombre }), "Id", "Nombre", estadoId);

        // Keep Search params
        ViewBag.CurrentSearch = search ?? "";
        ViewBag.CurrentCategoria = categoriaId;
        ViewBag.CurrentUbicacion = ubicacionId;
        ViewBag.CurrentEstado = estadoId;

        var list = await _service.GetAllAsync(search, categoriaId, ubicacionId, estadoId);
        return View(list);
    }

    // GET: Articulos/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var articulo = await _service.GetByIdAsync(id.Value);
        if (articulo == null) return NotFound();

        return View(articulo);
    }

    // GET: Articulos/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new ArticuloViewModel());
    }

    // POST: Articulos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticuloViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            if (await _service.ExistsCodigoAsync(viewModel.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un artículo con este código.");
            }
            else
            {
                var createdBy = User.Identity?.Name ?? "Sistema";
                var result = await _service.CreateAsync(viewModel, createdBy);

                if (result)
                {
                    TempData["SuccessMessage"] = "Artículo registrado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Ocurrió un error al guardar el artículo.");
            }
        }

        await LoadDropdownsAsync();
        return View(viewModel);
    }

    // GET: Articulos/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var articulo = await _service.GetByIdAsync(id.Value);
        if (articulo == null) return NotFound();

        await LoadDropdownsAsync();
        return View(articulo);
    }

    // POST: Articulos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticuloViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        if (ModelState.IsValid)
        {
            if (await _service.ExistsCodigoAsync(viewModel.Codigo, id))
            {
                ModelState.AddModelError("Codigo", "Ya existe otro artículo con este código.");
            }
            else
            {
                var result = await _service.UpdateAsync(viewModel);

                if (result)
                {
                    TempData["SuccessMessage"] = "Artículo actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "No se pudo actualizar el artículo.");
            }
        }

        await LoadDropdownsAsync();
        return View(viewModel);
    }

    // POST: Articulos/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Artículo eliminado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo eliminado el artículo.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDropdownsAsync()
    {
        var categorias = await _service.GetCategoriasAsync();
        ViewBag.Categorias = new SelectList(categorias.Select(c => new { c.Id, c.Nombre }), "Id", "Nombre");

        var ubicaciones = await _service.GetUbicacionesAsync();
        ViewBag.Ubicaciones = new SelectList(ubicaciones.Select(u => new { u.Id, u.Nombre }), "Id", "Nombre");
        
        var estados = await _service.GetEstadosAsync();
        ViewBag.Estados = new SelectList(estados.Select(e => new { e.Id, e.Nombre }), "Id", "Nombre");
    }
}
