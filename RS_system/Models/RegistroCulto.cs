using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Registro de ofrendas de un culto específico
/// </summary>
[Table("registros_culto")]
public class RegistroCulto
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("fecha")]
    [Required]
    public DateOnly Fecha { get; set; }
    
    [Column("observaciones")]
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;
    
    // Navigation property
    public virtual ICollection<Ofrenda> Ofrendas { get; set; } = new List<Ofrenda>();
    
    // Calculated properties
    [NotMapped]
    public decimal TotalOfrendas => Ofrendas?.Sum(o => o.Monto) ?? 0;
    
    [NotMapped]
    public decimal TotalDescuentos => Ofrendas?.Sum(o => o.TotalDescuentos) ?? 0;
    
    [NotMapped]
    public decimal MontoNeto => TotalOfrendas - TotalDescuentos;
}
