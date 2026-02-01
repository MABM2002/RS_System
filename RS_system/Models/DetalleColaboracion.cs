using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("detalle_colaboraciones", Schema = "public")]
public class DetalleColaboracion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("colaboracion_id")]
    public long ColaboracionId { get; set; }
    
    [Required]
    [Column("tipo_colaboracion_id")]
    public long TipoColaboracionId { get; set; }
    
    [Required]
    [Range(1, 12)]
    [Column("mes")]
    public int Mes { get; set; }
    
    [Required]
    [Range(2000, 2100)]
    [Column("anio")]
    public int Anio { get; set; }
    
    [Required]
    [Column("monto")]
    public decimal Monto { get; set; }
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("ColaboracionId")]
    public Colaboracion Colaboracion { get; set; } = null!;
    
    [ForeignKey("TipoColaboracionId")]
    public TipoColaboracion TipoColaboracion { get; set; } = null!;
}
