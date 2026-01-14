using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;

namespace Rs_system.Services;

public class MiembroService : IMiembroService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public MiembroService(ApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<IEnumerable<MiembroViewModel>> GetAllAsync()
    {
        return await _context.Miembros
            .Include(m => m.Persona)
            .Include(m => m.GrupoTrabajo)
            .Where(m => !m.Eliminado && m.Activo)
            .OrderBy(m => m.Persona.Apellidos)
            .ThenBy(m => m.Persona.Nombres)
            .Select(m => new MiembroViewModel
            {
                Id = m.Id,
                Nombres = m.Persona.Nombres,
                Apellidos = m.Persona.Apellidos,
                FechaNacimiento = m.Persona.FechaNacimiento,
                BautizadoEspirituSanto = m.BautizadoEspirituSanto,
                Direccion = m.Persona.Direccion,
                FechaIngresoCongregacion = m.FechaIngresoCongregacion,
                Telefono = m.Persona.Telefono,
                TelefonoEmergencia = m.TelefonoEmergencia,
                GrupoTrabajoId = m.GrupoTrabajoId,
                GrupoTrabajoNombre = m.GrupoTrabajo != null ? m.GrupoTrabajo.Nombre : null,
                Activo = m.Activo,
                FotoUrl = m.Persona.FotoUrl
            })
            .ToListAsync();
    }

    public async Task<MiembroViewModel?> GetByIdAsync(long id)
    {
        var miembro = await _context.Miembros
            .Include(m => m.Persona)
            .Include(m => m.GrupoTrabajo)
            .FirstOrDefaultAsync(m => m.Id == id && !m.Eliminado);

        if (miembro == null)
            return null;

        return new MiembroViewModel
        {
            Id = miembro.Id,
            Nombres = miembro.Persona.Nombres,
            Apellidos = miembro.Persona.Apellidos,
            FechaNacimiento = miembro.Persona.FechaNacimiento,
            BautizadoEspirituSanto = miembro.BautizadoEspirituSanto,
            Direccion = miembro.Persona.Direccion,
            FechaIngresoCongregacion = miembro.FechaIngresoCongregacion,
            Telefono = miembro.Persona.Telefono,
            TelefonoEmergencia = miembro.TelefonoEmergencia,
            GrupoTrabajoId = miembro.GrupoTrabajoId,
            GrupoTrabajoNombre = miembro.GrupoTrabajo?.Nombre,
            Activo = miembro.Activo,
            FotoUrl = miembro.Persona.FotoUrl
        };
    }

    public async Task<bool> CreateAsync(MiembroViewModel viewModel, string createdBy, IFormFile? fotoFile = null)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Handle photo upload
                string? fotoUrl = null;
                if (fotoFile != null)
                {
                    fotoUrl = await _fileStorageService.SaveFileAsync(fotoFile, "miembros");
                }

                // 1. Create Persona
                var persona = new Persona
                {
                    Nombres = viewModel.Nombres,
                    Apellidos = viewModel.Apellidos,
                    FechaNacimiento = viewModel.FechaNacimiento,
                    Direccion = viewModel.Direccion,
                    Telefono = viewModel.Telefono,
                    FotoUrl = fotoUrl,
                    Activo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                };

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                // 2. Create Miembro linked to Persona
                var miembro = new Miembro
                {
                    PersonaId = persona.Id,
                    BautizadoEspirituSanto = viewModel.BautizadoEspirituSanto,
                    FechaIngresoCongregacion = viewModel.FechaIngresoCongregacion,
                    TelefonoEmergencia = viewModel.TelefonoEmergencia,
                    GrupoTrabajoId = viewModel.GrupoTrabajoId,
                    Activo = viewModel.Activo,
                    CreadoPor = createdBy,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                };

                _context.Miembros.Add(miembro);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });

            return true;
        }
        catch (Exception ex)
        {
            // Log exception here if logger is available
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(long id, MiembroViewModel viewModel, IFormFile? fotoFile = null)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var miembro = await _context.Miembros
                    .Include(m => m.Persona)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.Eliminado);

                if (miembro == null)
                    throw new InvalidOperationException("Miembro no encontrado");

                // Handle photo upload
                if (fotoFile != null)
                {
                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(miembro.Persona.FotoUrl))
                    {
                        await _fileStorageService.DeleteFileAsync(miembro.Persona.FotoUrl);
                    }
                    
                    // Save new photo
                    miembro.Persona.FotoUrl = await _fileStorageService.SaveFileAsync(fotoFile, "miembros");
                }

                // Update Persona
                miembro.Persona.Nombres = viewModel.Nombres;
                miembro.Persona.Apellidos = viewModel.Apellidos;
                miembro.Persona.FechaNacimiento = viewModel.FechaNacimiento;
                miembro.Persona.Direccion = viewModel.Direccion;
                miembro.Persona.Telefono = viewModel.Telefono;
                miembro.Persona.ActualizadoEn = DateTime.UtcNow;

                // Update Miembro
                miembro.BautizadoEspirituSanto = viewModel.BautizadoEspirituSanto;
                miembro.FechaIngresoCongregacion = viewModel.FechaIngresoCongregacion;
                miembro.TelefonoEmergencia = viewModel.TelefonoEmergencia;
                miembro.GrupoTrabajoId = viewModel.GrupoTrabajoId;
                miembro.Activo = viewModel.Activo;
                miembro.ActualizadoEn = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var miembro = await _context.Miembros.FindAsync(id);
            if (miembro == null || miembro.Eliminado)
                return false;

            miembro.Eliminado = true;
            miembro.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<(long Id, string Nombre)>> GetGruposTrabajoAsync()
    {
        return await _context.GruposTrabajo
            .Where(g => g.Activo)
            .OrderBy(g => g.Nombre)
            .Select(g => new ValueTuple<long, string>(g.Id, g.Nombre))
            .ToListAsync();
    }
}
