using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("tipos_colaboracion", Schema = "public")]
public class TipoColaboracion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;
    
    [Column("descripcion")]
    public string? Descripcion { get; set; }
    
    [Column("monto_sugerido")]
    [Required]
    public decimal MontoSugerido { get; set; }
    
    [Column("activo")]
    public bool Activo { get; set; } = true;
    
    [Column("orden")]
    public int Orden { get; set; }
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<DetalleColaboracion> Detalles { get; set; } = new List<DetalleColaboracion>();
}
