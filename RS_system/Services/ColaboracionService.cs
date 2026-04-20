using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using Rs_system.Data;
using Rs_system.Models;
using Rs_system.Models.ViewModels;
using System.Globalization;

namespace Rs_system.Services;

public class ColaboracionService : IColaboracionService
{
    private readonly ApplicationDbContext _context;
    
    public ColaboracionService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<TipoColaboracion>> GetTiposActivosAsync()
    {
        return await _context.TiposColaboracion
            .Where(t => t.Activo)
            .OrderBy(t => t.Orden)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<TipoColaboracion?> GetTipoByIdAsync(long id)
    {
        return await _context.TiposColaboracion
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }
    
    public async Task<Colaboracion> RegistrarColaboracionAsync(
        RegistrarColaboracionViewModel model,
        string registradoPor)
    {

        try
        {
            // Validar que el rango de fechas sea válido
            var fechaInicial = new DateTime(model.AnioInicial, model.MesInicial, 1);
            var fechaFinal = new DateTime(model.AnioFinal, model.MesFinal, 1);

            if (fechaFinal < fechaInicial)
            {
                throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial");
            }

            // Get or create colaboracion head for today
            //var head = await GetOrCreateColaboracionHeadForDateAsync(DateTime.UtcNow, registradoPor);
            
            var head = await GetOrCreateColaboracionHeadForIdAsync(model.IdJornada, registradoPor);

            if (head == null)
            {
                throw new InvalidOperationException("No se pudo crear o obtener la jornada de colaboración.");
            }

            // Verificar que la jornada no esté cerrada
            if (head.EsCerrado)
            {
                throw new InvalidOperationException($"La jornada del {head.Fecha:dd/MM/yyyy} está cerrada. No se pueden agregar más colaboraciones.");
            }

            // Obtener información de los tipos seleccionados
            var tiposColaboracion = await _context.TiposColaboracion
                .Where(t => model.TiposSeleccionados.Contains(t.Id))
                .ToListAsync();

            // Generar todos los meses en el rango
            var mesesAPagar = GenerarRangoMeses(
                model.AnioInicial, model.MesInicial,
                model.AnioFinal, model.MesFinal);

            // Crear colaboración principal
            var colaboracion = new Colaboracion
            {
                MiembroId = model.MiembroId,
                ColaboracionHeadId = head.Id,
                FechaRegistro = DateTime.UtcNow,
                MontoTotal = model.MontoTotal,
                Observaciones = model.Observaciones,
                RegistradoPor = registradoPor,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };

            // Distribuir el monto total entre los meses y tipos
            var detalles = DistribuirMonto(
                model.MontoTotal,
                tiposColaboracion,
                mesesAPagar,
                model.TipoPrioritario);

            foreach (var detalle in detalles)
            {
                colaboracion.Detalles.Add(detalle);
            }

            _context.Colaboraciones.Add(colaboracion);

            // Guardar la colaboración primero
            await _context.SaveChangesAsync();

            // Update the head total using the entity framework to maintain consistency
            head.Total += model.MontoTotal;
            head.ActualizadoEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return colaboracion;
        }
        catch(Exception ex)
        {
            // Log the inner exception for better diagnostics
            if (ex.InnerException != null)
            {
                throw new Exception($"Error al registrar colaboración: {ex.InnerException.Message}", ex.InnerException);
            }
            throw new Exception($"Error al registrar colaboración: {ex.Message}", ex);
        }
    }
    
    private List<DetalleColaboracion> DistribuirMonto(
        decimal montoTotal,
        List<TipoColaboracion> tipos,
        List<(int anio, int mes)> meses,
        long? tipoPrioritario)
    {
        var detalles = new List<DetalleColaboracion>();
        var montoRestante = montoTotal;
        
        foreach (var (anio, mes) in meses)
        {
            if (montoRestante <= 0) break;

            // Ordenar tipos para este mes: Prioritario al inicio
            var tiposOrdenados = new List<TipoColaboracion>();
            
            if (tipoPrioritario.HasValue)
            {
                var prio = tipos.FirstOrDefault(t => t.Id == tipoPrioritario.Value);
                if (prio != null)
                {
                    tiposOrdenados.Add(prio);
                    tiposOrdenados.AddRange(tipos.Where(t => t.Id != tipoPrioritario.Value));
                }
                else
                {
                    tiposOrdenados.AddRange(tipos);
                }
            }
            else
            {
                tiposOrdenados.AddRange(tipos);
            }

            foreach (var tipo in tiposOrdenados)
            {
                if (montoRestante <= 0) break;

                var montoAAsignar = Math.Min(tipo.MontoSugerido, montoRestante);
                
                
                if (montoAAsignar > 0)
                {
                    detalles.Add(new DetalleColaboracion
                    {
                        TipoColaboracionId = tipo.Id,
                        Mes = mes,
                        Anio = anio,
                        Monto = montoAAsignar,
                        CreadoEn = DateTime.UtcNow
                    });
                    
                    montoRestante -= montoAAsignar;
                }
            }
        }
        
        return detalles;
    }
    
    public async Task<List<UltimoPagoViewModel>> GetUltimosPagosPorMiembroAsync(long miembroId)
    {
        // Obtener todos los detalles agrupados por tipo para encontrar la fecha máxima
        var detalles = await _context.DetalleColaboraciones
            .Include(d => d.Colaboracion)
            .Include(d => d.TipoColaboracion)
            .Where(d => d.Colaboracion.MiembroId == miembroId)
            .ToListAsync();
            
        var resultado = detalles
            .GroupBy(d => d.TipoColaboracion)
            .Select(g =>
            {
                // Encontrar el registro con el mes/año más reciente
                var ultimo = g.OrderByDescending(d => d.Anio).ThenByDescending(d => d.Mes).FirstOrDefault();
                if (ultimo == null) return null;

                return new UltimoPagoViewModel
                {
                    TipoId = g.Key.Id,
                    NombreTipo = g.Key.Nombre,
                    UltimoMes = ultimo.Mes,
                    UltimoAnio = ultimo.Anio,
                    FechaUltimoPago = ultimo.Colaboracion.FechaRegistro
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
            
        // Asegurar que retornamos todos los tipos activos, incluso si no tienen pagos
        var tiposActivos = await GetTiposActivosAsync();
        var listaFinal = new List<UltimoPagoViewModel>();
        
        foreach (var tipo in tiposActivos)
        {
            var pago = resultado.FirstOrDefault(r => r.TipoId == tipo.Id);
            if (pago != null)
            {
                listaFinal.Add(pago);
            }
            else
            {
                listaFinal.Add(new UltimoPagoViewModel
                {
                    TipoId = tipo.Id,
                    NombreTipo = tipo.Nombre,
                    UltimoMes = 0, // No hay pagos
                    UltimoAnio = 0
                });
            }
        }
        
        return listaFinal;
    }

    private List<(int anio, int mes)> GenerarRangoMeses(
        int anioInicial, int mesInicial,
        int anioFinal, int mesFinal)
    {
        var meses = new List<(int, int)>();
        var fecha = new DateTime(anioInicial, mesInicial, 1);
        var fechaFin = new DateTime(anioFinal, mesFinal, 1);
        
        while (fecha <= fechaFin)
        {
            meses.Add((fecha.Year, fecha.Month));
            fecha = fecha.AddMonths(1);
        }
        
        return meses;
    }
    
    public async Task<List<Colaboracion>> GetColaboracionesRecientesAsync(int cantidad = 50)
    {
        return await _context.Colaboraciones
            .Include(c => c.Miembro)
                .ThenInclude(m => m.Persona)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.TipoColaboracion)
            .OrderByDescending(c => c.FechaRegistro)
            .Take(cantidad)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<Colaboracion?> GetColaboracionByIdAsync(long id)
    {
        

        return await _context.Colaboraciones
            .Include(c => c.Miembro)
                .ThenInclude(m => m.Persona)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.TipoColaboracion)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<ReporteColaboracionesViewModel> GenerarReportePorFechasAsync(
        DateTime fechaInicio, 
        DateTime fechaFin)
    {
        var colaboraciones = await _context.Colaboraciones
            .Include(c => c.Miembro)
                .ThenInclude(m => m.Persona)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.TipoColaboracion)
            .Where(c => c.FechaRegistro >= fechaInicio && c.FechaRegistro <= fechaFin)
            .OrderByDescending(c => c.FechaRegistro)
            .AsNoTracking()
            .ToListAsync();
        
        var reporte = new ReporteColaboracionesViewModel
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            TotalRecaudado = colaboraciones.Sum(c => c.MontoTotal)
        };
        
        // Desglose por tipo
        var desglosePorTipo = colaboraciones
            .SelectMany(c => c.Detalles)
            .GroupBy(d => d.TipoColaboracion.Nombre)
            .Select(g => new DesglosePorTipo
            {
                TipoNombre = g.Key,
                CantidadMeses = g.Count(),
                TotalRecaudado = g.Sum(d => d.Monto)
            })
            .OrderBy(d => d.TipoNombre)
            .ToList();
        
        reporte.DesglosePorTipos = desglosePorTipo;
        
        // Detalle de movimientos
        var movimientos = colaboraciones.Select(c => new DetalleMovimiento
        {
            ColaboracionId = c.Id,
            Fecha = c.FechaRegistro,
            NombreMiembro = $"{c.Miembro.Persona.Nombres} {c.Miembro.Persona.Apellidos}",
            TiposColaboracion = string.Join(", ", c.Detalles.Select(d => d.TipoColaboracion.Nombre).Distinct()),
            PeriodoCubierto = ObtenerPeriodoCubierto(c.Detalles.ToList()),
            Monto = c.MontoTotal
        }).ToList();
        
        reporte.Movimientos = movimientos;
        
        return reporte;
    }
    
    private string ObtenerPeriodoCubierto(List<DetalleColaboracion> detalles)
    {
        if (!detalles.Any()) return "";
        
        var ordenados = detalles.OrderBy(d => d.Anio).ThenBy(d => d.Mes).ToList();
        var primero = ordenados.First();
        var ultimo = ordenados.Last();
        
        var cultura = new CultureInfo("es-ES");
        
        if (primero.Anio == ultimo.Anio && primero.Mes == ultimo.Mes)
        {
            return new DateTime(primero.Anio, primero.Mes, 1).ToString("MMMM yyyy", cultura);
        }
        
        return $"{new DateTime(primero.Anio, primero.Mes, 1).ToString("MMM yyyy", cultura)} - " +
               $"{new DateTime(ultimo.Anio, ultimo.Mes, 1).ToString("MMM yyyy", cultura)}";
    }
    
    public async Task<EstadoCuentaViewModel> GenerarEstadoCuentaAsync(long miembroId)
    {
        var miembro = await _context.Miembros
            .Include(m => m.Persona)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == miembroId);
        
        if (miembro == null)
            throw new Exception("Miembro no encontrado");
        
        var colaboraciones = await _context.Colaboraciones
            .Include(c => c.Detalles)
                .ThenInclude(d => d.TipoColaboracion)
            .Where(c => c.MiembroId == miembroId)
            .AsNoTracking()
            .ToListAsync();
        
        var estado = new EstadoCuentaViewModel
        {
            MiembroId = miembroId,
            NombreMiembro = $"{miembro.Persona.Nombres} {miembro.Persona.Apellidos}",
            FechaConsulta = DateTime.Now,
            TotalAportado = colaboraciones.Sum(c => c.MontoTotal)
        };
        
        // Agrupar por tipo
        var historialPorTipo = colaboraciones
            .SelectMany(c => c.Detalles.Select(d => new { Detalle = d, FechaRegistro = c.FechaRegistro }))
            .GroupBy(x => x.Detalle.TipoColaboracion.Nombre)
            .Select(g => new HistorialPorTipo
            {
                TipoNombre = g.Key,
                TotalTipo = g.Sum(x => x.Detalle.Monto),
                Registros = g.Select(x => new RegistroMensual
                {
                    Mes = x.Detalle.Mes,
                    Anio = x.Detalle.Anio,
                    Monto = x.Detalle.Monto,
                    FechaRegistro = x.FechaRegistro
                })
                .OrderBy(r => r.Anio)
                .ThenBy(r => r.Mes)
                .ToList()
            })
            .OrderBy(h => h.TipoNombre)
            .ToList();
        
        estado.HistorialPorTipos = historialPorTipo;

        return estado;
    }

    // New methods for ColaboracionHead (Master-Detail pattern)
    public async Task<List<ColaboracionHeadIndexViewModel>> GetColaboracionHeadsRecientesAsync(int cantidad = 50)
    {
        return await _context.ColaboracionHeads
            .Include(h => h.Colaboraciones)
            .OrderByDescending(h => h.Fecha)
            .Take(cantidad)
            .Select(h => new ColaboracionHeadIndexViewModel
            {
                Id = h.Id,
                Fecha = h.Fecha,
                Total = h.Total,
                CantidadColaboraciones = h.Colaboraciones.Count,
                CreadoPor = h.CreadoPor,
                CreadoEn = h.CreadoEn,
                EsCerrado = h.EsCerrado,
                FechaCierre = h.FechaCierre,
                CerradoPor = h.CerradoPor
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ColaboracionHeadDetalleViewModel?> GetColaboracionHeadByIdAsync(long id)
    {
        var head = await _context.ColaboracionHeads
            .Include(h => h.Colaboraciones)
                .ThenInclude(c => c.Miembro)
                    .ThenInclude(m => m.Persona)
            .Include(h => h.Colaboraciones)
                .ThenInclude(c => c.Detalles)
                    .ThenInclude(d => d.TipoColaboracion)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (head == null)
            return null;

        return new ColaboracionHeadDetalleViewModel
        {
            Id = head.Id,
            Fecha = head.Fecha,
            Total = head.Total,
            CreadoPor = head.CreadoPor,
            CreadoEn = head.CreadoEn,
            EsCerrado = head.EsCerrado,
            FechaCierre = head.FechaCierre,
            CerradoPor = head.CerradoPor,
            Colaboraciones = head.Colaboraciones.Select(c => new ColaboracionDetalleViewModel
            {
                Id = c.Id,
                MiembroNombre = $"{c.Miembro.Persona.Nombres} {c.Miembro.Persona.Apellidos}",
                MontoTotal = c.MontoTotal,
                Observaciones = c.Observaciones,
                RegistradoPor = c.RegistradoPor,
                FechaRegistro = c.FechaRegistro,
                TiposColaboracion = c.Detalles
                    .Select(d => d.TipoColaboracion.Nombre)
                    .Distinct()
                    .ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Obtiene o Crea un colaboracionHead segun la fecha
    /// </summary>
    /// <param name="fecha"></param>
    /// <param name="creadoPor"></param>
    /// <returns></returns>
    public async Task<ColaboracionHead?> GetOrCreateColaboracionHeadForDateAsync(DateTime fecha, string creadoPor)
    {
        var fechaDate = fecha.Date;

        var head = await _context.ColaboracionHeads
            .FirstOrDefaultAsync(h => h.Fecha.Date == fechaDate);

        if (head == null)
        {
            head = new ColaboracionHead
            {
                Fecha = fechaDate,
                Total = 0,
                CreadoPor = creadoPor,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };

            _context.ColaboracionHeads.Add(head);
            await _context.SaveChangesAsync(); // Save immediately to get the ID
        }

        return head;
    }

    /// <summary>
    /// Obtiene o Crea un colaboracionHead segun la fecha
    /// </summary>
    /// <param name="fecha"></param>
    /// <param name="creadoPor"></param>
    /// <returns></returns>
    public async Task<ColaboracionHead?> GetOrCreateColaboracionHeadForIdAsync(int IdJornada, string creadoPor)
    {
        // var fechaDate = fecha.Date;

        var head = await _context.ColaboracionHeads
            .FirstOrDefaultAsync(h => h.Id == IdJornada);

        if (head == null)
        {
            head = new ColaboracionHead
            {
                Fecha = DateTime.UtcNow,
                Total = 0,
                CreadoPor = creadoPor,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };

            _context.ColaboracionHeads.Add(head);
            await _context.SaveChangesAsync(); // Save immediately to get the ID
        }

        return head;
    }

    public async Task<CierreDiarioResult> RealizarCierreDiarioAsync(long colaboracionHeadId, string cerradoPor)
    {   
        try
        {
            // 1. Obtener y validar la ColaboracionHead
            var colaboracionHead = await _context.ColaboracionHeads
                .FirstOrDefaultAsync(h => h.Id == colaboracionHeadId);

            if (colaboracionHead == null)
            {
                throw new ArgumentException($"ColaboracionHead con ID {colaboracionHeadId} no encontrada");
            }

            if (colaboracionHead.EsCerrado)
            {
                throw new InvalidOperationException($"La jornada del {colaboracionHead.Fecha:dd/MM/yyyy} ya está cerrada");
            }

            if (colaboracionHead.Total <= 0)
            {
                throw new InvalidOperationException($"No se puede cerrar una jornada con total 0. Total actual: {colaboracionHead.Total:N2}");
            }

            // 2. Bloquear la ColaboracionHead (marcar como cerrada)
            colaboracionHead.EsCerrado = true;
            colaboracionHead.FechaCierre = DateTime.UtcNow;
            colaboracionHead.CerradoPor = cerradoPor;
            colaboracionHead.ActualizadoEn = DateTime.UtcNow;

            // 3. Verificar/Crear ReporteMensualGeneral para el mes/año
            var mes = colaboracionHead.Fecha.Month;
            var anio = colaboracionHead.Fecha.Year;

            var reporteMensual = await _context.ReportesMensualesGenerales
                .FirstOrDefaultAsync(r => r.Mes == mes && r.Anio == anio);

            if (reporteMensual == null)
            {
                reporteMensual = new ReporteMensualGeneral
                {
                    Mes = mes,
                    Anio = anio,
                    SaldoInicial = 0,
                    FechaCreacion = DateTime.UtcNow,
                    Cerrado = false
                };

                _context.ReportesMensualesGenerales.Add(reporteMensual);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID
            }

            // 4. Crear MovimientoGeneral para el cierre diario
            var movimientoGeneral = new MovimientoGeneral
            {
                ReporteMensualGeneralId = reporteMensual.Id,
                CategoriaIngresoId = 1, // Categoría fija para colaboraciones
                Monto = colaboracionHead.Total,
                Fecha = colaboracionHead.Fecha,
                Tipo = (int)TipoMovimientoGeneral.Ingreso,
                Descripcion = $"Cierre diario de colaboraciones - {colaboracionHead.Fecha:dd/MM/yyyy}",
                NumeroComprobante = $"CIERRE-{colaboracionHead.Fecha:yyyyMMdd}"
            };

            _context.MovimientosGenerales.Add(movimientoGeneral);
            _context.Entry(colaboracionHead).State = EntityState.Modified;
            // _context.Entry(movimientoGeneral).State = EntityState.Modified;
            // 5. Guardar todos los cambios
            await _context.SaveChangesAsync();

            // 6. Retornar resultado exitoso
            return new CierreDiarioResult
            {
                Success = true,
                Message = $"Cierre diario realizado exitosamente para la jornada del {colaboracionHead.Fecha:dd/MM/yyyy}",
                ColaboracionHeadId = colaboracionHead.Id,
                Fecha = colaboracionHead.Fecha,
                TotalCerrado = colaboracionHead.Total,
                ReporteMensualGeneralId = reporteMensual.Id,
                MovimientoGeneralId = movimientoGeneral.Id
            };
        }
        catch (Exception ex)
        {   
            return new CierreDiarioResult
            {
                Success = false,
                Message = $"Error al realizar el cierre diario: {ex.Message}",
                Error = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    // This method is kept for backward compatibility
    public Task<Colaboracion> RegistrarColabo2racionAsync(RegistrarColaboracionViewModel model, string registradoPor)
    {
        return RegistrarColaboracionAsync(model, registradoPor);
    }
}
