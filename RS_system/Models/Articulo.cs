using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("articulos")]
public class Articulo
{
    public enum TipoControlInventario
    {
        UNITARIO, // 1 record = 1 physical item (Laptop, Projector)
        LOTE      // 1 record = N items (Chairs, Cables)
    }

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("tipo_control")]
    [Required]
    public string TipoControl { get; set; } = nameof(TipoControlInventario.UNITARIO);

    [Column("cantidad_global")]
    public int CantidadGlobal { get; set; } = 1; // Cache/Total for LOTE. Always 1 for UNITARIO.

    [Column("codigo")]
    [Required(ErrorMessage = "El código es obligatorio")]
    [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres")]
    public string Codigo { get; set; } = string.Empty;

    [Column("nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
    public string? Descripcion { get; set; }

    [Column("marca")]
    [StringLength(100)]
    public string? Marca { get; set; }

    [Column("modelo")]
    [StringLength(100)]
    public string? Modelo { get; set; }

    [Column("numero_serie")]
    [StringLength(100)]
    public string? NumeroSerie { get; set; }

    [Column("precio")]
    [Range(0, 99999999.99)]
    public decimal Precio { get; set; } = 0;

    [Column("fecha_adquisicion")]
    public DateOnly? FechaAdquisicion { get; set; }

    [Column("imagen_url")]
    public string? ImagenUrl { get; set; }

    // Foreign Keys

    [Column("categoria_id")]
    public int CategoriaId { get; set; }

    [Column("estado_id")]
    public int EstadoId { get; set; }

    [Column("ubicacion_id")]
    public int UbicacionId { get; set; }

    // Audit & Control

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

    // Navigation Properties

    [ForeignKey("CategoriaId")]
    public virtual Categoria? Categoria { get; set; }

    [ForeignKey("EstadoId")]
    public virtual EstadoArticulo? Estado { get; set; }

    [ForeignKey("UbicacionId")]
    public virtual Ubicacion? Ubicacion { get; set; }
}
