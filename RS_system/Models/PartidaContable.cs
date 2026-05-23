using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Encabezado de Partida Contable (Asiento de Diario).
/// Agrupa uno o más DetallePartidaContable que en conjunto deben balancear Débito = Crédito.
/// </summary>
[Table("partidas_contables")]
public class PartidaContable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha")]
    [Required]
    public DateTime Fecha { get; set; }

    /// <summary>Número de referencia o comprobante (ej. "AS-2026-0001", "CIERRE-20260521").</summary>
    [Column("referencia")]
    [StringLength(50)]
    public string? Referencia { get; set; }

    /// <summary>Descripción o glosa del asiento.</summary>
    [Column("descripcion")]
    [StringLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>FK al período contable al que pertenece esta partida.</summary>
    [Column("periodo_contable_id")]
    public long? PeriodoContableId { get; set; }

    [ForeignKey("PeriodoContableId")]
    public virtual PeriodoContable? Periodo { get; set; }

    /// <summary>Indica si esta partida está bloqueada (período cerrado).</summary>
    [Column("cerrada")]
    public bool Cerrada { get; set; } = false;

    /// <summary>FK opcional al MovimientoGeneral que originó esta partida (trazabilidad).</summary>
    [Column("movimiento_general_id")]
    public long? MovimientoGeneralId { get; set; }

    [ForeignKey("MovimientoGeneralId")]
    public virtual MovimientoGeneral? MovimientoGeneral { get; set; }

    /// <summary>FK opcional al ContabilidadRegistro que originó esta partida (trazabilidad).</summary>
    [Column("contabilidad_registro_id")]
    public long? ContabilidadRegistroId { get; set; }

    [ForeignKey("ContabilidadRegistroId")]
    public virtual ContabilidadRegistro? ContabilidadRegistro { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<DetallePartidaContable> Detalles { get; set; } = new List<DetallePartidaContable>();

    // Helpers
    [NotMapped]
    public decimal TotalDebito => Detalles?.Sum(d => d.Debito) ?? 0;

    [NotMapped]
    public decimal TotalCredito => Detalles?.Sum(d => d.Credito) ?? 0;
}
