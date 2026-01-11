using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Descuento aplicado a una ofrenda (diezmo, asignaciones, etc.)
/// </summary>
[Table("descuentos_ofrenda")]
public class DescuentoOfrenda
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("ofrenda_id")]
    [Required]
    public long OfrendaId { get; set; }
    
    [Column("monto")]
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Monto { get; set; }
    
    [Column("concepto")]
    [Required]
    [StringLength(200)]
    public string Concepto { get; set; } = string.Empty;
    
    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;
    
    // Navigation property
    [ForeignKey("OfrendaId")]
    public virtual Ofrenda? Ofrenda { get; set; }
}
