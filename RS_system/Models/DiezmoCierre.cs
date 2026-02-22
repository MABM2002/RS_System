using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Agregado raíz del módulo de diezmos.
/// Representa un período/corte de diezmos (la fecha la elige el operador libremente).
/// Un cierre por fecha (UNIQUE en fecha).
/// </summary>
[Table("diezmo_cierres")]
public class DiezmoCierre
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Fecha del cierre. UNIQUE — no pueden existir dos cierres para el mismo día.</summary>
    [Column("fecha")]
    [Required]
    public DateOnly Fecha { get; set; }

    [Column("cerrado")]
    public bool Cerrado { get; set; } = false;

    [Column("fecha_cierre")]
    public DateTime? FechaCierre { get; set; }

    [Column("cerrado_por")]
    [StringLength(100)]
    public string? CerradoPor { get; set; }

    [Column("observaciones")]
    [StringLength(500)]
    public string? Observaciones { get; set; }

    // ── Totales calculados (persistidos para consulta rápida en el listado) ──
    [Column("total_recibido", TypeName = "numeric(12,2)")]
    public decimal TotalRecibido { get; set; } = 0;

    [Column("total_cambio", TypeName = "numeric(12,2)")]
    public decimal TotalCambio { get; set; } = 0;

    [Column("total_neto", TypeName = "numeric(12,2)")]
    public decimal TotalNeto { get; set; } = 0;

    [Column("total_salidas", TypeName = "numeric(12,2)")]
    public decimal TotalSalidas { get; set; } = 0;

    [Column("saldo_final", TypeName = "numeric(12,2)")]
    public decimal SaldoFinal { get; set; } = 0;

    // ── Auditoría ──
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_por")]
    [StringLength(100)]
    public string? ActualizadoPor { get; set; }

    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;

    // ── Navegación ──
    public virtual ICollection<DiezmoDetalle> Detalles { get; set; } = new List<DiezmoDetalle>();
    public virtual ICollection<DiezmoSalida>  Salidas  { get; set; } = new List<DiezmoSalida>();
}
