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

    public async Task<(int SuccessCount, List<string> Errors)> ImportarMiembrosAsync(Stream csvStream, string createdBy)
    {
        int successCount = 0;
        var errors = new List<string>();
        int rowNumber = 1; // 1-based, starting at header

        using var reader = new StreamReader(csvStream);
        
        // Read valid groups for validation
        var validGroupIds = await _context.GruposTrabajo
            .Where(g => g.Activo)
            .Select(g => g.Id)
            .ToListAsync();
        var validGroupIdsSet = new HashSet<long>(validGroupIds);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            rowNumber++;
            
            // Skip header if it looks like one (simple check or just assume first row is header)
            // The prompt implies a specific format, we'll assume the first row IS the header based on standard CSV practices,
            // but if the user provides a file without header it might be an issue. 
            // However, usually "loading a csv" implies a header. 
            // I'll skip the first row (header) in the loop logic by adding a check.
            if (rowNumber == 2) continue; // Skip header row (rowNumber started at 1, so first ReadLine is row 1 (header), loop increments to 2) 
                                          // Wait, if I increment rowNumber AFTER reading, then:
                                          // Start: rowNumber=1.
                                          // ReadLine (Header). rowNumber becomes 2. 
                                          // So if rowNumber == 2, it means we just read the header. Correct.
            
            // Parse CSV line
            var values = ParseCsvLine(line);
            
            // Expected columns:
            // 0: Nombres
            // 1: Apellidos
            // 2: Fecha Nacimiento
            // 3: Fecha Ingreso Congregacion
            // 4: Telefono
            // 5: Telefono Emergencia
            // 6: Direccion
            // 7: Grupo de trabajo (ID)
            // 8: Bautizado en El Espiritu Santo (Si/No or True/False)
            // 9: Activo (Si/No or True/False)

            if (values.Count < 10)
            {
                errors.Add($"Fila {rowNumber}: Número de columnas insuficiente. Se esperaban 10, se encontraron {values.Count}.");
                continue;
            }

            try
            {
                // Validation and Parsing
                var nombres = values[0].Trim();
                var apellidos = values[1].Trim();
                if (string.IsNullOrEmpty(nombres) || string.IsNullOrEmpty(apellidos))
                {
                    errors.Add($"Fila {rowNumber}: Nombres y Apellidos son obligatorios.");
                    continue;
                }

                DateOnly? fechaNacimiento = ParseDate(values[2]);
                DateOnly? fechaIngreso = ParseDate(values[3]);
                
                var telefono = values[4].Trim();
                var telefonoEmergencia = values[5].Trim();
                var direccion = values[6].Trim();
                
                if (!long.TryParse(values[7], out long grupoId))
                {
                    errors.Add($"Fila {rowNumber}: ID de Grupo de trabajo inválido '{values[7]}'.");
                    continue;
                }
                
                if (!validGroupIdsSet.Contains(grupoId))
                {
                    errors.Add($"Fila {rowNumber}: Grupo de trabajo con ID {grupoId} no existe o no está activo.");
                    continue;
                }

                bool bautizado = ParseBool(values[8]);
                bool activo = ParseBool(values[9]);

                // Create Logic
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try 
                    {
                        var persona = new Persona
                        {
                            Nombres = nombres,
                            Apellidos = apellidos,
                            FechaNacimiento = fechaNacimiento,
                            Direccion = string.IsNullOrEmpty(direccion) ? null : direccion,
                            Telefono = string.IsNullOrEmpty(telefono) ? null : telefono,
                            Activo = activo,
                            CreadoEn = DateTime.UtcNow,
                            ActualizadoEn = DateTime.UtcNow
                        };
                        _context.Personas.Add(persona);
                        await _context.SaveChangesAsync();

                        var miembro = new Miembro
                        {
                            PersonaId = persona.Id,
                            BautizadoEspirituSanto = bautizado,
                            FechaIngresoCongregacion = fechaIngreso,
                            TelefonoEmergencia = string.IsNullOrEmpty(telefonoEmergencia) ? null : telefonoEmergencia,
                            GrupoTrabajoId = grupoId,
                            Activo = activo,
                            CreadoPor = createdBy,
                            CreadoEn = DateTime.UtcNow,
                            ActualizadoEn = DateTime.UtcNow
                        };
                        _context.Miembros.Add(miembro);
                        await _context.SaveChangesAsync();
                        
                        await transaction.CommitAsync();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Fila {rowNumber}: Error al guardar en base de datos: {ex.Message}");
                        // Transaction rolls back automatically on dispose if not committed
                    }
                });
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {rowNumber}: Error inesperado: {ex.Message}");
            }
        }

        return (successCount, errors);
    }

    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        string currentValue = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue);
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }
        values.Add(currentValue);
        
        // Remove surrounding quotes if present
        for (int i = 0; i < values.Count; i++)
        {
            var val = values[i].Trim();
            if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
            {
                values[i] = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
            }
            else 
            {
                values[i] = val;
            }
        }

        return values;
    }

    private DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateOnly.TryParse(value, out var date)) return date;
        if (DateTime.TryParse(value, out var dt)) return DateOnly.FromDateTime(dt);
        return null; // Or throw depending on strictness, currently lenient
    }

    private bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var val = value.Trim().ToLower();
        return val == "1" || val == "true" || val == "si" || val == "yes" || val == "s" || val == "verdadero";
    }

    public async Task<PaginatedViewModel<MiembroViewModel>> GetPaginatedAsync(int page, int pageSize, string? searchQuery = null)
    {
        // Ensure valid page and pageSize
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max limit

        // Start with base query
        var query = _context.Miembros
            .Include(m => m.Persona)
            .Include(m => m.GrupoTrabajo)
            .Where(m => !m.Eliminado && m.Activo);

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(m =>
                m.Persona.Nombres.ToLower().Contains(search) ||
                m.Persona.Apellidos.ToLower().Contains(search) ||
                (m.Persona.Nombres + " " + m.Persona.Apellidos).ToLower().Contains(search)
            );
        }

        // Get total count for pagination
        var totalItems = await query.CountAsync();

        // Get paginated items
        var items = await query
            .OrderBy(m => m.Persona.Apellidos)
            .ThenBy(m => m.Persona.Nombres)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PaginatedViewModel<MiembroViewModel>
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            SearchQuery = searchQuery
        };
    }
}
