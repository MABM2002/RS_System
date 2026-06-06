using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

/// <summary>ViewModel for the daily journal index list.</summary>
public class DiarioFinancieroIndexViewModel
{
    public long Id { get; set; }

    [Display(Name = "Fecha")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime Fecha { get; set; }

    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Abierto";

    [Display(Name = "Total Ingresos")]
    [DisplayFormat(DataFormatString = "${0:N2}")]
    public decimal TotalIngresos { get; set; }

    [Display(Name = "Total Egresos")]
    [DisplayFormat(DataFormatString = "${0:N2}")]
    public decimal TotalEgresos { get; set; }

    [Display(Name = "Saldo del Día")]
    [DisplayFormat(DataFormatString = "${0:N2}")]
    public decimal SaldoDia { get; set; }

    [Display(Name = "Movimientos")]
    public int CantidadMovimientos { get; set; }

    [Display(Name = "Creado por")]
    public string? CreadoPor { get; set; }
}

/// <summary>ViewModel for the detail page: header + movements.</summary>
public class DiarioFinancieroDetalleViewModel
{
    public DiarioFinancieroCabecera Cabecera { get; set; } = null!;
    public List<CategoriaIngreso> CategoriasIngreso { get; set; } = new();
    public List<CategoriaEgreso> CategoriasEgreso { get; set; } = new();
    public List<MetodoPago> MetodosPago { get; set; } = new();
}

/// <summary>Filters for the reporting page.</summary>
public class DiarioFinancieroFiltroViewModel
{
    [Display(Name = "Fecha Inicio")]
    [DataType(DataType.Date)]
    public DateTime? FechaInicio { get; set; }

    [Display(Name = "Fecha Fin")]
    [DataType(DataType.Date)]
    public DateTime? FechaFin { get; set; }

    [Display(Name = "Tipo")]
    public int? Tipo { get; set; }   // 1 = Ingreso, 2 = Egreso

    [Display(Name = "Categoría Ingreso")]
    public long? CategoriaIngresoId { get; set; }

    [Display(Name = "Categoría Egreso")]
    public long? CategoriaEgresoId { get; set; }

    [Display(Name = "Método de Pago")]
    public long? MetodoPagoId { get; set; }
}

/// <summary>Report results with filter + aggregated data.</summary>
public class DiarioFinancieroReporteViewModel
{
    public DiarioFinancieroFiltroViewModel Filtro { get; set; } = new();
    public List<DiarioFinancieroDetalle> Movimientos { get; set; } = new();
    public decimal TotalIngresos { get; set; }
    public decimal TotalEgresos { get; set; }
    public decimal Saldo { get; set; }
    public List<CategoriaIngreso> CategoriasIngreso { get; set; } = new();
    public List<CategoriaEgreso> CategoriasEgreso { get; set; } = new();
    public List<MetodoPago> MetodosPago { get; set; } = new();

    // Church / organization info for report header
    public string NombreIglesia { get; set; } = "Iglesia";
    public string? DireccionIglesia { get; set; }
    public string? TelefonoIglesia { get; set; }
    public string? EmailIglesia { get; set; }
}

/// <summary>Input model for creating/editing a single movement via AJAX.</summary>
public class DiarioMovimientoInput
{
    public long Id { get; set; }
    public long CabeceraId { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public int Tipo { get; set; }
    public long? CategoriaIngresoId { get; set; }
    public long? CategoriaEgresoId { get; set; }
    public string? NumeroComprobante { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public long? MetodoPagoId { get; set; }
    public string? Tercero { get; set; }
    public string? Observaciones { get; set; }
}
