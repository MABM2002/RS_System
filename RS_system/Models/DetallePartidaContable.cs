using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Línea de detalle de una Partida Contable.
/// Contiene el débito o crédito imputado a una cuenta específica.
/// Regla: solo uno de Débito/Crédito debe ser > 0 por línea.
/// </summary>
[Table("detalles_partida_contable")]
public class DetallePartidaContable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("partida_contable_id")]
    [Required]
    public long PartidaContableId { get; set; }

    [ForeignKey("PartidaContableId")]
    public virtual PartidaContable Partida { get; set; } = null!;

    [Column("cuenta_contable_id")]
    [Required]
    public long CuentaContableId { get; set; }

    [ForeignKey("CuentaContableId")]
    public virtual CuentaContable Cuenta { get; set; } = null!;

    /// <summary>Monto del débito (debe ser >= 0).</summary>
    [Column("debito", TypeName = "decimal(18,2)")]
    public decimal Debito { get; set; }

    /// <summary>Monto del crédito (debe ser >= 0).</summary>
    [Column("credito", TypeName = "decimal(18,2)")]
    public decimal Credito { get; set; }

    /// <summary>Descripción opcional de la línea.</summary>
    [Column("descripcion")]
    [StringLength(300)]
    public string? Descripcion { get; set; }
}
