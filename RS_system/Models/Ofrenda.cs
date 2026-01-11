using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Ofrenda individual dentro de un registro de culto
/// </summary>
[Table("ofrendas")]
public class Ofrenda
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("registro_culto_id")]
    [Required]
    public long RegistroCultoId { get; set; }
    
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
    
    // Navigation properties
    [ForeignKey("RegistroCultoId")]
    public virtual RegistroCulto? RegistroCulto { get; set; }
    
    public virtual ICollection<DescuentoOfrenda> Descuentos { get; set; } = new List<DescuentoOfrenda>();
    
    // Calculated properties
    [NotMapped]
    public decimal TotalDescuentos => Descuentos?.Where(d => !d.Eliminado).Sum(d => d.Monto) ?? 0;
    
    [NotMapped]
    public decimal MontoNeto => Monto - TotalDescuentos;
}
