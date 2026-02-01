using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("categorias_ingreso")]
public class CategoriaIngreso
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [StringLength(255)]
    public string? Descripcion { get; set; }

    [Column("activa")]
    public bool Activa { get; set; } = true;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<MovimientoGeneral> Movimientos { get; set; } = new List<MovimientoGeneral>();
}
