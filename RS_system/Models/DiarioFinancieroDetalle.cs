using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("diario_financiero_detalles")]
public class DiarioFinancieroDetalle
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("cabecera_id")]
    [Required]
    public long CabeceraId { get; set; }

    [ForeignKey("CabeceraId")]
    public virtual DiarioFinancieroCabecera Cabecera { get; set; } = null!;

    [Column("fecha_movimiento")]
    [Required]
    public DateTime FechaMovimiento { get; set; }

    /// <summary>1 = Ingreso, 2 = Egreso</summary>
    [Column("tipo")]
    [Required]
    public int Tipo { get; set; }

    [Column("categoria_ingreso_id")]
    public long? CategoriaIngresoId { get; set; }

    [ForeignKey("CategoriaIngresoId")]
    public virtual CategoriaIngreso? CategoriaIngreso { get; set; }

    [Column("categoria_egreso_id")]
    public long? CategoriaEgresoId { get; set; }

    [ForeignKey("CategoriaEgresoId")]
    public virtual CategoriaEgreso? CategoriaEgreso { get; set; }

    [Column("numero_comprobante")]
    [StringLength(50)]
    public string? NumeroComprobante { get; set; }

    [Column("descripcion")]
    [Required]
    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Column("monto", TypeName = "decimal(18,2)")]
    [Required]
    public decimal Monto { get; set; }

    [Column("metodo_pago_id")]
    public long? MetodoPagoId { get; set; }

    [ForeignKey("MetodoPagoId")]
    public virtual MetodoPago? MetodoPago { get; set; }

    [Column("tercero")]
    [StringLength(200)]
    public string? Tercero { get; set; }

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    // Audit fields
    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("modificado_por")]
    [StringLength(100)]
    public string? ModificadoPor { get; set; }

    [Column("fecha_modificacion")]
    public DateTime? FechaModificacion { get; set; }

    // Navigation — attachments
    public virtual ICollection<DiarioFinancieroAdjunto> Adjuntos { get; set; } = new List<DiarioFinancieroAdjunto>();

    // Helpers
    [NotMapped]
    public bool EsIngreso => Tipo == 1;

    [NotMapped]
    public bool EsEgreso => Tipo == 2;

    [NotMapped]
    public string NombreCategoria => EsIngreso
        ? (CategoriaIngreso?.Nombre ?? "Sin Categoría")
        : (CategoriaEgreso?.Nombre ?? "Sin Categoría");

    [NotMapped]
    public string NombreTipo => EsIngreso ? "Ingreso" : "Egreso";
}
