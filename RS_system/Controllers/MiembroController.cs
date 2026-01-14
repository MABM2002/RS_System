using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models.ViewModels;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class MiembroController : Controller
{
    private readonly IMiembroService _miembroService;

    public MiembroController(IMiembroService miembroService)
    {
        _miembroService = miembroService;
    }

    // GET: Miembro
    public async Task<IActionResult> Index()
    {
        var miembros = await _miembroService.GetAllAsync();
        return View(miembros);
    }

    // GET: Miembro/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
            return NotFound();

        var miembro = await _miembroService.GetByIdAsync(id.Value);
        if (miembro == null)
            return NotFound();

        return View(miembro);
    }

    // GET: Miembro/Create
    public async Task<IActionResult> Create()
    {
        await LoadGruposTrabajoAsync();
        var viewModel = new MiembroViewModel
        {
            FechaIngresoCongregacion = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(viewModel);
    }

    // POST: Miembro/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MiembroViewModel viewModel, IFormFile? fotoFile)
    {
        if (ModelState.IsValid)
        {
            var createdBy = User.Identity?.Name ?? "Sistema";
            var success = await _miembroService.CreateAsync(viewModel, createdBy, fotoFile);

            if (success)
            {
                TempData["SuccessMessage"] = "Miembro creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Error al crear el miembro. Intente nuevamente.");
        }

        await LoadGruposTrabajoAsync();
        return View(viewModel);
    }

    // GET: Miembro/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
            return NotFound();

        var miembro = await _miembroService.GetByIdAsync(id.Value);
        if (miembro == null)
            return NotFound();

        await LoadGruposTrabajoAsync();
        return View(miembro);
    }

    // POST: Miembro/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, MiembroViewModel viewModel, IFormFile? fotoFile)
    {
        if (id != viewModel.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var success = await _miembroService.UpdateAsync(id, viewModel, fotoFile);

            if (success)
            {
                TempData["SuccessMessage"] = "Miembro actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Error al actualizar el miembro. Intente nuevamente.");
        }

        await LoadGruposTrabajoAsync();
        return View(viewModel);
    }

    // POST: Miembro/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var success = await _miembroService.DeleteAsync(id);

        if (success)
            TempData["SuccessMessage"] = "Miembro eliminado exitosamente.";
        else
            TempData["ErrorMessage"] = "Error al eliminar el miembro.";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadGruposTrabajoAsync()
    {
        var grupos = await _miembroService.GetGruposTrabajoAsync();
        ViewBag.GruposTrabajo = new SelectList(grupos.Select(g => new { g.Id, g.Nombre }), "Id", "Nombre");
    }
}
