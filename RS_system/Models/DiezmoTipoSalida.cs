using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Catálogo de tipos de salida del módulo de diezmos
/// (Entrega al Pastor, Gastos Administrativos, Misiones, etc.)
/// </summary>
[Table("diezmo_tipos_salida")]
public class DiezmoTipoSalida
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [StringLength(300)]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Marca este tipo como la entrega oficial al pastor.
    /// Permite sugerirlo/forzarlo automáticamente al cerrar con saldo pendiente.
    /// </summary>
    [Column("es_entrega_pastor")]
    public bool EsEntregaPastor { get; set; } = false;

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("creado_por")]
    [StringLength(100)]
    public string? CreadoPor { get; set; }

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    // Navegación
    public virtual ICollection<DiezmoSalida> Salidas { get; set; } = new List<DiezmoSalida>();
}
