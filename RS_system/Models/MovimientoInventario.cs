using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

public enum TipoMovimiento
{
    ENTRADA,      // Nueva adquisición (aunque se crea al crear art, podría usarse para reingresos)
    SALIDA,       // Salida temporal
    TRASLADO,     // Cambio de ubicación
    BAJA,         // Retiro permanente (daño, robo, venta)
    REPARACION,   // Envío a taller
    AJUSTE,       // Corrección de inventario
    CAMBIO_ESTADO, // Cambio de condición física
    PRESTAMO,    // Préstamo a persona
    DEVOLUCION
}

[Table("movimientos_inventario")]
public class MovimientoInventario
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("articulo_id")]
    [Required]
    public int ArticuloId { get; set; }

    [Column("tipo_movimiento")]
    [Required]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Column("cantidad")]
    public int Cantidad { get; set; } = 1; // Default 1 for UNITARIO

    [Column("fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Ubicaciones
    [Column("ubicacion_origen_id")]
    public int? UbicacionOrigenId { get; set; }

    [Column("ubicacion_destino_id")]
    public int? UbicacionDestinoId { get; set; }

    // Estados
    [Column("estado_anterior_id")]
    public int? EstadoAnteriorId { get; set; }

    [Column("estado_nuevo_id")]
    public int? EstadoNuevoId { get; set; }
    
    [Column("TipMov")]
    public int? TipMov { get; set; }

    [Column("observacion")]
    [StringLength(500)]
    public string? Observacion { get; set; }

    [Column("usuario_id")]
    [StringLength(100)]
    public string? UsuarioId { get; set; } // Username or User ID

    // Navigation Properties
    [ForeignKey("ArticuloId")]
    public virtual Articulo? Articulo { get; set; }

    [ForeignKey("UbicacionOrigenId")]
    public virtual Ubicacion? UbicacionOrigen { get; set; }

    [ForeignKey("UbicacionDestinoId")]
    public virtual Ubicacion? UbicacionDestino { get; set; }

    [ForeignKey("EstadoAnteriorId")]
    public virtual EstadoArticulo? EstadoAnterior { get; set; }

    [ForeignKey("EstadoNuevoId")]
    public virtual EstadoArticulo? EstadoNuevo { get; set; }
}
