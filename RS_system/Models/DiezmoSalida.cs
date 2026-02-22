using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Salida de fondos registrada contra un cierre de diezmos.
/// Incluye entregas al pastor, gastos administrativos, misiones, etc.
/// </summary>
[Table("diezmo_salidas")]
public class DiezmoSalida
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("diezmo_cierre_id")]
    public long DiezmoCierreId { get; set; }

    [Column("tipo_salida_id")]
    public long TipoSalidaId { get; set; }

    [Column("beneficiario_id")]
    public long? BeneficiarioId { get; set; }

    [Column("monto", TypeName = "numeric(12,2)")]
    [Required]
    public decimal Monto { get; set; }

    [Column("concepto")]
    [Required]
    [StringLength(300)]
    public string Concepto { get; set; } = string.Empty;

    /// <summary>Correlativo de recibo asignado al momento de generar el comprobante.</summary>
    [Column("numero_recibo")]
    [StringLength(30)]
    public string? NumeroRecibo { get; set; }

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

    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;

    // ── Navegación ──
    [ForeignKey("DiezmoCierreId")]
    public virtual DiezmoCierre DiezmoCierre { get; set; } = null!;

    [ForeignKey("TipoSalidaId")]
    public virtual DiezmoTipoSalida TipoSalida { get; set; } = null!;

    [ForeignKey("BeneficiarioId")]
    public virtual DiezmoBeneficiario? Beneficiario { get; set; }
}
