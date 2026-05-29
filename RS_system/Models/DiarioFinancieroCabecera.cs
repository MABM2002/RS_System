using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("diario_financiero_cabeceras")]
public class DiarioFinancieroCabecera
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("fecha")]
    [Required]
    public DateTime Fecha { get; set; }

    [Column("estado")]
    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "Abierto";

    [Column("total_ingresos", TypeName = "decimal(18,2)")]
    public decimal TotalIngresos { get; set; }

    [Column("total_egresos", TypeName = "decimal(18,2)")]
    public decimal TotalEgresos { get; set; }

    [Column("saldo_dia", TypeName = "decimal(18,2)")]
    public decimal SaldoDia { get; set; }

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    // Audit fields
    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("modificado_por")]
    [StringLength(100)]
    public string? ModificadoPor { get; set; }

    [Column("fecha_modificacion")]
    public DateTime? FechaModificacion { get; set; }

    // Navigation
    public virtual ICollection<DiarioFinancieroDetalle> Detalles { get; set; } = new List<DiarioFinancieroDetalle>();

    // Helpers
    [NotMapped]
    public bool EstaAbierto => Estado == "Abierto";

    [NotMapped]
    public bool EstaCerrado => Estado == "Cerrado";
}
