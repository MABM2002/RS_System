using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Personas o entidades que pueden recibir salidas de diezmos
/// (pastor, tesorero, organismos externos, etc.)
/// </summary>
[Table("diezmo_beneficiarios")]
public class DiezmoBeneficiario
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [StringLength(300)]
    public string? Descripcion { get; set; }

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

    [Column("actualizado_por")]
    [StringLength(100)]
    public string? ActualizadoPor { get; set; }

    [Column("idpersona")]
    public long? IdPersona { get; set; }

    [ForeignKey("IdPersona")]
    public virtual Persona? Persona { get; set; }

    // Navegación
    public virtual ICollection<DiezmoSalida> Salidas { get; set; } = new List<DiezmoSalida>();
}
