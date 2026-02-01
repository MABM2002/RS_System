using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("estados_articulos")]
public class EstadoArticulo
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [StringLength(200, ErrorMessage = "La descripción no puede exceder los 200 caracteres")]
    public string? Descripcion { get; set; }

    [Column("color")]
    [StringLength(20)]
    public string? Color { get; set; } = "secondary"; // success, warning, danger, info, primary, secondary

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }
}
