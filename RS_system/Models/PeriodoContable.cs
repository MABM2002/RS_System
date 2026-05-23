using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Período contable mensual. Controla el cierre y bloqueo de ediciones.
/// </summary>
[Table("periodos_contables")]
public class PeriodoContable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("mes")]
    [Required]
    [Range(1, 12)]
    public int Mes { get; set; }

    [Column("anio")]
    [Required]
    public int Anio { get; set; }

    [Column("fecha_inicio")]
    public DateTime FechaInicio { get; set; }

    [Column("fecha_fin")]
    public DateTime FechaFin { get; set; }

    /// <summary>Indica si el período está cerrado. Si true, no se permiten nuevas partidas ni ediciones.</summary>
    [Column("cerrado")]
    public bool Cerrado { get; set; } = false;

    /// <summary>Saldo inicial del período (arrastre del período anterior).</summary>
    [Column("saldo_inicial", TypeName = "decimal(18,2)")]
    public decimal SaldoInicial { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("fecha_cierre")]
    public DateTime? FechaCierre { get; set; }

    [Column("cerrado_por")]
    [StringLength(100)]
    public string? CerradoPor { get; set; }

    // Navigation
    public virtual ICollection<PartidaContable> Partidas { get; set; } = new List<PartidaContable>();

    // Helper
    [NotMapped]
    public string NombreMes => new DateTime(Anio, Mes, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
}
