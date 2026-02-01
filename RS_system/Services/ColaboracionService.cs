using Microsoft.EntityFrameworkCore;
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
        // Validar que el rango de fechas sea válido
        var fechaInicial = new DateTime(model.AnioInicial, model.MesInicial, 1);
        var fechaFinal = new DateTime(model.AnioFinal, model.MesFinal, 1);
        
        if (fechaFinal < fechaInicial)
        {
            throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial");
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
        await _context.SaveChangesAsync();
        
        return colaboracion;
    }
    
    private List<DetalleColaboracion> DistribuirMonto(
        decimal montoTotal,
        List<TipoColaboracion> tipos,
        List<(int anio, int mes)> meses,
        long? tipoPrioritario)
    {
        var detalles = new List<DetalleColaboracion>();
        var montoRestante = montoTotal;
        
        // Estrategia: Mes a Mes
        // Para cada mes, intentamos cubrir los tipos (Prioritario primero)
        
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

                // Determinar cuánto asignar
                // Intentamos cubrir el monto sugerido completo
                var montoAAsignar = Math.Min(tipo.MontoSugerido, montoRestante);
                
                // Si es un monto muy pequeño (ej: residuo), igual lo asignamos para no perderlo,
                // salvo que queramos reglas estrictas de "solo completos".
                // Por ahora asignamos lo que haya.
                
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
}
