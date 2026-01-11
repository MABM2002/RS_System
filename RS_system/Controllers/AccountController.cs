using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rs_system.Models.ViewModels;
using Rs_system.Services;
using Rs_system.Data;
using Microsoft.EntityFrameworkCore;

namespace Rs_system.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;
    private readonly ApplicationDbContext _context;
    
    public AccountController(IAuthService authService, ILogger<AccountController> logger, ApplicationDbContext context)
    {
        _authService = authService;
        _logger = logger;
        _context = context;
    }
    
    // GET: /Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
    
    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var usuario = await _authService.ValidateUserAsync(model.NombreUsuario, model.Contrasena);
        
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
            return View(model);
        }
        
        // Get user roles
        var roles = await _authService.GetUserRolesAsync(usuario.Id);
        
        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Email, usuario.Email),
            new("FullName", usuario.Persona?.NombreCompleto ?? usuario.NombreUsuario)
        };
        
        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permissions as claims
        var permissions = await _context.RolesUsuario
            .Where(ru => ru.UsuarioId == usuario.Id) // Changed user.Id to usuario.Id
            .Join(_context.RolesPermisos, ru => ru.RolId, rp => rp.RolId, (ru, rp) => rp)
            .Join(_context.Permisos, rp => rp.PermisoId, p => p.Id, (rp, p) => p)
            .Select(p => p.Codigo)
            .Distinct()
            .ToListAsync();

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RecordarMe,
            ExpiresUtc = model.RecordarMe 
                ? DateTimeOffset.UtcNow.AddDays(30) 
                : DateTimeOffset.UtcNow.AddHours(8)
        };
        
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
        
        // Update last login
        await _authService.UpdateLastLoginAsync(usuario.Id);
        
        _logger.LogInformation("User {Username} logged in", usuario.NombreUsuario);
        
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        
        return RedirectToAction("Index", "Home");
    }
    
    // GET: /Account/Register
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        
        return View();
    }
    
    // POST: /Account/Register
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var (success, message, _) = await _authService.RegisterUserAsync(model);
        
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }
        
        TempData["SuccessMessage"] = "¡Registro exitoso! Ahora puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }
    
    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }
    
    // GET: /Account/AccessDenied
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
