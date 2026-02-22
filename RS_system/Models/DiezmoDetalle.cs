using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Diezmo individual aportado por un miembro dentro de un cierre.
/// MontoNeto = MontoEntregado - CambioEntregado (calculado por el sistema).
/// </summary>
[Table("diezmo_detalles")]
public class DiezmoDetalle
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("diezmo_cierre_id")]
    public long DiezmoCierreId { get; set; }

    [Column("miembro_id")]
    public long MiembroId { get; set; }

    /// <summary>Monto físico que el miembro entregó (puede incluir cambio).</summary>
    [Column("monto_entregado", TypeName = "numeric(12,2)")]
    [Required]
    public decimal MontoEntregado { get; set; }

    /// <summary>Cambio devuelto al miembro.</summary>
    [Column("cambio_entregado", TypeName = "numeric(12,2)")]
    public decimal CambioEntregado { get; set; } = 0;

    /// <summary>Diezmo neto real = MontoEntregado - CambioEntregado. Calculado por el sistema.</summary>
    [Column("monto_neto", TypeName = "numeric(12,2)")]
    public decimal MontoNeto { get; set; }

    [Column("observaciones")]
    [StringLength(300)]
    public string? Observaciones { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // ── Auditoría ──
    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_por")]
    [StringLength(100)]
    public string? ActualizadoPor { get; set; }

    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;

    // ── Navegación ──
    [ForeignKey("DiezmoCierreId")]
    public virtual DiezmoCierre DiezmoCierre { get; set; } = null!;

    [ForeignKey("MiembroId")]
    public virtual Miembro Miembro { get; set; } = null!;
}
