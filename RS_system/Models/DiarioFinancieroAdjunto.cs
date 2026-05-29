using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("diario_financiero_adjuntos")]
public class DiarioFinancieroAdjunto
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("detalle_id")]
    [Required]
    public long DetalleId { get; set; }

    [ForeignKey("DetalleId")]
    public virtual DiarioFinancieroDetalle Detalle { get; set; } = null!;

    [Column("nombre_archivo")]
    [Required]
    [StringLength(255)]
    public string NombreArchivo { get; set; } = string.Empty;

    [Column("ruta_archivo")]
    [Required]
    [StringLength(500)]
    public string RutaArchivo { get; set; } = string.Empty;

    [Column("tipo_contenido")]
    [StringLength(100)]
    public string? TipoContenido { get; set; }

    [Column("fecha_subida")]
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
}
