using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

public enum TipoMovimientoGeneral
{
    Ingreso = 1,
    Egreso = 2
}

[Table("movimientos_generales")]
public class MovimientoGeneral
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("reporte_mensual_general_id")]
    public long? ReporteMensualGeneralId { get; set; }

    [ForeignKey("ReporteMensualGeneralId")]
    public virtual ReporteMensualGeneral? ReporteMensualGeneral { get; set; }

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

    [Column("monto", TypeName = "decimal(18,2)")]
    [Required]
    public decimal Monto { get; set; }

    [Column("fecha")]
    [Required]
    public DateTime Fecha { get; set; }

    [Column("descripcion")]
    [StringLength(200)]
    public string Descripcion { get; set; } = string.Empty;

    [Column("numero_comprobante")]
    [StringLength(50)]
    public string? NumeroComprobante { get; set; }

    // Navigation property
    public virtual ICollection<MovimientoGeneralAdjunto> Adjuntos { get; set; } = new List<MovimientoGeneralAdjunto>();
}
