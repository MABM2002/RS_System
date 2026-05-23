using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rs_system.Models;
using Rs_system.Services;

namespace Rs_system.Controllers;

[Authorize]
public class AccountTypesController : Controller
{
    private readonly IContabilidadPartidaDobleService _contabilidadService;

    public AccountTypesController(IContabilidadPartidaDobleService contabilidadService)
    {
        _contabilidadService = contabilidadService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tipos = await _contabilidadService.GetAllAccountTypesAsync();
        return View(tipos);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Naturalezas = new SelectList(
            new[] { new { Value = (int)NaturalezaCuenta.Deudora, Text = "Deudora" },
                    new { Value = (int)NaturalezaCuenta.Acreedora, Text = "Acreedora" } },
            "Value", "Text");

        ViewBag.CategoriasReporte = new SelectList(
            new[] { new { Value = (int)CategoriaReporte.Balance, Text = "Balance General" },
                    new { Value = (int)CategoriaReporte.Resultado, Text = "Estado de Resultados" } },
            "Value", "Text");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountType accountType)
    {
        if (!ModelState.IsValid)
        {
            SetupViewBags();
            return View(accountType);
        }

        try
        {
            await _contabilidadService.CreateAccountTypeAsync(accountType);
            TempData["Success"] = $"Tipo de cuenta '{accountType.Nombre}' creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        SetupViewBags();
        return View(accountType);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tipo = await _contabilidadService.GetAccountTypeByIdAsync(id);
        if (tipo == null) return NotFound();

        SetupViewBags();
        return View(tipo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountType accountType)
    {
        if (id != accountType.Id) return BadRequest();
        if (!ModelState.IsValid) { SetupViewBags(); return View(accountType); }

        try
        {
            await _contabilidadService.UpdateAccountTypeAsync(accountType);
            TempData["Success"] = "Tipo de cuenta actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }

        SetupViewBags();
        return View(accountType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _contabilidadService.DeleteAccountTypeAsync(id);
            TempData["Success"] = "Tipo de cuenta eliminado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private void SetupViewBags()
    {
        ViewBag.Naturalezas = new SelectList(
            new[] { new { Value = (int)NaturalezaCuenta.Deudora, Text = "Deudora" },
                    new { Value = (int)NaturalezaCuenta.Acreedora, Text = "Acreedora" } },
            "Value", "Text");

        ViewBag.CategoriasReporte = new SelectList(
            new[] { new { Value = (int)CategoriaReporte.Balance, Text = "Balance General" },
                    new { Value = (int)CategoriaReporte.Resultado, Text = "Estado de Resultados" } },
            "Value", "Text");
    }
}
