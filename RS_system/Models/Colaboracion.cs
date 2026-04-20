using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("colaboraciones", Schema = "public")]
public class Colaboracion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("miembro_id")]
    public long MiembroId { get; set; }

    [Column("colaboracion_head_id")]
    public long? ColaboracionHeadId { get; set; }

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("monto_total")]
    public decimal MontoTotal { get; set; }

    [Column("observaciones")]
    public string? Observaciones { get; set; }

    [MaxLength(100)]
    [Column("registrado_por")]
    public string? RegistradoPor { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("MiembroId")]
    public Miembro Miembro { get; set; } = null!;

    [ForeignKey("ColaboracionHeadId")]
    public ColaboracionHead? ColaboracionHead { get; set; }

    public ICollection<DetalleColaboracion> Detalles { get; set; } = new List<DetalleColaboracion>();
}
