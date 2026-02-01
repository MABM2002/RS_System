using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("categorias")]
public class Categoria
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
    public string? Descripcion { get; set; }

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
