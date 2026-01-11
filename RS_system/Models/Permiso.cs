using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("permisos")]
public class Permiso
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("modulo_id")]
    [Required]
    public int ModuloId { get; set; }

    [ForeignKey("ModuloId")]
    public virtual Modulo? Modulo { get; set; }

    [Column("codigo")]
    [Required]
    [StringLength(100)]
    public string Codigo { get; set; } = string.Empty;

    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("url")]
    public string? Url { get; set; }

    [Column("icono")]
    public string? Icono { get; set; }

    [Column("orden")]
    public int Orden { get; set; } = 0;

    [Column("es_menu")]
    public bool EsMenu { get; set; } = true;

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
