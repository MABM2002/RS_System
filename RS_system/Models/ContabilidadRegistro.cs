using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

public enum TipoMovimientoContable
{
    Ingreso = 1,
    Egreso = 2
}

[Table("contabilidad_registros")]
public class ContabilidadRegistro
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("reporte_mensual_id")]
    public long? ReporteMensualId { get; set; }

    [ForeignKey("ReporteMensualId")]
    public virtual ReporteMensualContable? ReporteMensual { get; set; }

    [Column("grupo_trabajo_id")]
    [Required]
    public long GrupoTrabajoId { get; set; }

    [ForeignKey("GrupoTrabajoId")]
    public virtual GrupoTrabajo GrupoTrabajo { get; set; }

    [Column("tipo")]
    [Required]
    public TipoMovimientoContable Tipo { get; set; }

    [Column("monto", TypeName = "decimal(18,2)")]
    [Required]
    public decimal Monto { get; set; }


    [Column("fecha")]
    [Required]
    public DateTime Fecha { get; set; }

    [Column("descripcion")]
    [StringLength(200)]
    public string Descripcion { get; set; } = string.Empty;
}

