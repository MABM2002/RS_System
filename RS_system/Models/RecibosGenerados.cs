using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Registro de recibo generado para una salida de diezmo.
/// </summary>
[Table("recibos_generados")]
public class RecibosGenerados
{
    [Key]
    [Column("num_recibo")]
    [StringLength(30)]
    public string NumRecibo { get; set; } = string.Empty;

    [Column("nombre_beneficiario")]
    [Required]
    [StringLength(150)]
    public string NombreBeneficiario { get; set; } = string.Empty;

    [Column("nombre_iglesia")]
    [Required]
    [StringLength(200)]
    public string NombreIglesia { get; set; } = string.Empty;

    [Column("monto_decimal", TypeName = "numeric(15,2)")]
    [Required]
    public decimal MontoDecimal { get; set; }

    [Column("monto_texto")]
    [Required]
    public string MontoTexto { get; set; } = string.Empty;

    [Column("dia")]
    [Required]
    public int Dia { get; set; }

    [Column("mes")]
    [Required]
    public int Mes { get; set; }

    [Column("anio")]
    [Required]
    public int Anio { get; set; }

    [Column("concepto")]
    public string? Concepto { get; set; }

    [Column("id_salida")]
    [Required]
    public long IdSalida { get; set; }

    [Column("fecha_generacion")]
    [Required]
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;

    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("actualizado_en")]
    public DateTime? ActualizadoEn { get; set; }

    [Column("creado_por")]
    [Required]
    [StringLength(100)]
    public string CreadoPor { get; set; } = string.Empty;

    // ── Navegación ──
    [ForeignKey("IdSalida")]
    public virtual DiezmoSalida? DiezmoSalida { get; set; }
}
