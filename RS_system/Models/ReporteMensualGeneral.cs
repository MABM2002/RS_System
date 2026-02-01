using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("reportes_mensuales_generales")]
public class ReporteMensualGeneral
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("mes")]
    [Required]
    public int Mes { get; set; }

    [Column("anio")]
    [Required]
    public int Anio { get; set; }

    [Column("saldo_inicial", TypeName = "decimal(18,2)")]
    public decimal SaldoInicial { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("cerrado")]
    public bool Cerrado { get; set; } = false;

    // Navigation property for details
    public virtual ICollection<MovimientoGeneral> Movimientos { get; set; } = new List<MovimientoGeneral>();

    // Helper properties for display
    [NotMapped]
    public string NombreMes => new DateTime(Anio, Mes, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
}
