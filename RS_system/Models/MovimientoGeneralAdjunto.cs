using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("movimientos_generales_adjuntos")]
public class MovimientoGeneralAdjunto
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("movimiento_general_id")]
    [Required]
    public long MovimientoGeneralId { get; set; }

    [ForeignKey("MovimientoGeneralId")]
    public virtual MovimientoGeneral MovimientoGeneral { get; set; }

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
