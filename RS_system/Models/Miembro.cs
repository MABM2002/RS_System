using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("miembros")]
public class Miembro
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("persona_id")]
    public long PersonaId { get; set; }
    
    [Column("bautizado_espiritu_santo")]
    public bool BautizadoEspirituSanto { get; set; } = false;
    
    [Column("fecha_ingreso_congregacion")]
    public DateOnly? FechaIngresoCongregacion { get; set; }
    
    [Column("telefono_emergencia")]
    [StringLength(20)]
    public string? TelefonoEmergencia { get; set; }
    
    [Column("grupo_trabajo_id")]
    public long? GrupoTrabajoId { get; set; }
    
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
    
    // Navigation properties
    [ForeignKey("PersonaId")]
    public virtual Persona Persona { get; set; } = null!;
    
    [ForeignKey("GrupoTrabajoId")]
    public virtual GrupoTrabajo? GrupoTrabajo { get; set; }
}
