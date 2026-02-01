using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("existencias")]
public class Existencia
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("articulo_id")]
    public int ArticuloId { get; set; }

    [Column("ubicacion_id")]
    public int UbicacionId { get; set; }

    [Column("cantidad")]
    public int Cantidad { get; set; } = 0;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ArticuloId")]
    public virtual Articulo? Articulo { get; set; }

    [ForeignKey("UbicacionId")]
    public virtual Ubicacion? Ubicacion { get; set; }
}
