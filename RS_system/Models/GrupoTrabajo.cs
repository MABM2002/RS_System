using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("grupos_trabajo")]
public class GrupoTrabajo
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    
    [Column("descripcion")]
    public string? Descripcion { get; set; }
    
    [Column("activo")]
    public bool Activo { get; set; } = true;
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Miembro> Miembros { get; set; } = new List<Miembro>();
}
