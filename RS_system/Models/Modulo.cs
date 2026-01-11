using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("modulos")]
public class Modulo
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("icono")]
    [StringLength(50)]
    public string? Icono { get; set; }

    [Column("orden")]
    public int Orden { get; set; } = 0;

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("parent_id")]
    public int? ParentId { get; set; }

    // Navigation properties
    [ForeignKey("ParentId")]
    public virtual Modulo? Parent { get; set; }
    
    public virtual ICollection<Modulo> SubModulos { get; set; } = new List<Modulo>();
    
    public virtual ICollection<Permiso> Permisos { get; set; } = new List<Permiso>();
}
