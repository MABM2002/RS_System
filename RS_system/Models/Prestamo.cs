using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("prestamos")]
public class Prestamo
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("articulo_id")]
    [Required]
    public int ArticuloId { get; set; }

    [Column("cantidad")]
    [Required]
    public int Cantidad { get; set; }

    [Column("persona_nombre")]
    [Required]
    [StringLength(200)]
    public string PersonaNombre { get; set; } = string.Empty;

    [Column("persona_identificacion")]
    [StringLength(50)]
    public string? PersonaIdentificacion { get; set; }

    [Column("fecha_prestamo")]
    public DateTime FechaPrestamo { get; set; } = DateTime.UtcNow;

    [Column("fecha_devolucion_estimada")]
    public DateTime? FechaDevolucionEstimada { get; set; }

    [Column("fecha_devolucion_real")]
    public DateTime? FechaDevolucionReal { get; set; }

    [Column("estado")]
    [Required]
    public string Estado { get; set; } = "ACTIVO"; // ACTIVO, DEVUELTO, ATRASADO

    [Column("observacion")]
    [StringLength(500)]
    public string? Observacion { get; set; }

    [Column("usuario_id")]
    [StringLength(100)]
    public string? UsuarioId { get; set; }

    // Navigation Properties
    [ForeignKey("ArticuloId")]
    public virtual Articulo? Articulo { get; set; }

    // Navigation Property for detailed items
    public virtual ICollection<PrestamoDetalle> Detalles { get; set; } = new List<PrestamoDetalle>();
}

[Table("prestamo_detalles")]
public class PrestamoDetalle
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("prestamo_id")]
    [Required]
    public long PrestamoId { get; set; }

    [Column("codigo_articulo_individual")]
    [Required]
    [StringLength(100)]
    public string CodigoArticuloIndividual { get; set; } = string.Empty;

    [Column("estado")]
    [Required]
    public string Estado { get; set; } = "PRESTADO"; // PRESTADO, DEVUELTO

    [Column("fecha_devolucion")]
    public DateTime? FechaDevolucion { get; set; }

    [Column("observacion")]
    [StringLength(300)]
    public string? Observacion { get; set; }

    // Navigation Properties
    [ForeignKey("PrestamoId")]
    public virtual Prestamo? Prestamo { get; set; }
}