using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using BCrypt.Net;

using Microsoft.AspNetCore.Authorization;

namespace Rs_system.Controllers;

[Authorize]
public class UsuarioController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsuarioController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Usuario
    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.Persona)
            .Include(u => u.RolesUsuario)
                .ThenInclude(ru => ru.Rol)
            .ToListAsync();
        return View(usuarios);
    }

    // GET: Usuario/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _context.RolesSistema.ToListAsync();
        return View(new UsuarioViewModel());
    }

    // POST: Usuario/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioViewModel model)
    {
        if (string.IsNullOrEmpty(model.Contrasena))
        {
            ModelState.AddModelError("Contrasena", "La contraseña es requerida para nuevos usuarios");
        }

        if (ModelState.IsValid)
        {
            if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario))
            {
                ModelState.AddModelError("NombreUsuario", "El nombre de usuario ya está en uso");
                ViewBag.Roles = await _context.RolesSistema.ToListAsync();
                return View(model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico ya está en uso");
                ViewBag.Roles = await _context.RolesSistema.ToListAsync();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var persona = new Persona
                {
                    Nombres = model.Nombres,
                    Apellidos = model.Apellidos,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    Activo = true
                };

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = new Usuario
                {
                    PersonaId = persona.Id,
                    NombreUsuario = model.NombreUsuario,
                    Email = model.Email,
                    HashContrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena),
                    Activo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Assign Roles
                if (model.SelectedRoles != null)
                {
                    foreach (var roleId in model.SelectedRoles)
                    {
                        _context.RolesUsuario.Add(new RolUsuario
                        {
                            UsuarioId = usuario.Id,
                            RolId = roleId,
                            AsignadoEn = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Ocurrió un error al crear el usuario.");
            }
        }
        ViewBag.Roles = await _context.RolesSistema.ToListAsync();
        return View(model);
    }

    // GET: Usuario/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null) return NotFound();

        var usuario = await _context.Usuarios
            .Include(u => u.Persona)
            .Include(u => u.RolesUsuario)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null) return NotFound();

        var model = new UsuarioViewModel
        {
            Id = usuario.Id,
            Nombres = usuario.Persona.Nombres,
            Apellidos = usuario.Persona.Apellidos,
            NombreUsuario = usuario.NombreUsuario,
            Email = usuario.Email,
            Telefono = usuario.Persona.Telefono,
            Activo = usuario.Activo,
            SelectedRoles = usuario.RolesUsuario.Select(ru => ru.RolId).ToList()
        };

        ViewBag.Roles = await _context.RolesSistema.ToListAsync();
        return View(model);
    }

    // POST: Usuario/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, UsuarioViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Persona)
                .Include(u => u.RolesUsuario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Persona
                usuario.Persona.Nombres = model.Nombres;
                usuario.Persona.Apellidos = model.Apellidos;
                usuario.Persona.Telefono = model.Telefono;
                usuario.Persona.ActualizadoEn = DateTime.UtcNow;

                // Update Usuario
                usuario.NombreUsuario = model.NombreUsuario;
                usuario.Email = model.Email;
                usuario.Activo = model.Activo;
                usuario.ActualizadoEn = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(model.Contrasena))
                {
                    usuario.HashContrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena);
                }

                // Update Roles
                _context.RolesUsuario.RemoveRange(usuario.RolesUsuario);
                if (model.SelectedRoles != null)
                {
                    foreach (var roleId in model.SelectedRoles)
                    {
                        _context.RolesUsuario.Add(new RolUsuario
                        {
                            UsuarioId = usuario.Id,
                            RolId = roleId,
                            AsignadoEn = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Ocurrió un error al actualizar el usuario.");
            }
        }
        ViewBag.Roles = await _context.RolesSistema.ToListAsync();
        return View(model);
    }

    // POST: Usuario/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(long id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario != null)
        {
            usuario.Activo = false;
            usuario.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool UsuarioExists(long id)
    {
        return _context.Usuarios.Any(e => e.Id == id);
    }
}
