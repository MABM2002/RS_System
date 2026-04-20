using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("colaboracion_heads", Schema = "public")]
public class ColaboracionHead
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Required]
    [Column("total")]
    public decimal Total { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    [Column("creado_por")]
    public string? CreadoPor { get; set; }

    [Column("es_cerrado")]
    public bool EsCerrado { get; set; } = false;

    [Column("fecha_cierre")]
    public DateTime? FechaCierre { get; set; }

    [MaxLength(100)]
    [Column("cerrado_por")]
    public string? CerradoPor { get; set; }

    // Navigation property
    public ICollection<Colaboracion> Colaboraciones { get; set; } = new List<Colaboracion>();
}
