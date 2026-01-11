using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("usuarios")]
public class Usuario
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("persona_id")]
    public long? PersonaId { get; set; }
    
    [Column("nombre_usuario")]
    [Required]
    [StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;
    
    [Column("email")]
    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Column("hash_contrasena")]
    [Required]
    public string HashContrasena { get; set; } = string.Empty;
    
    [Column("activo")]
    public bool Activo { get; set; } = true;
    
    [Column("ultimo_login")]
    public DateTime? UltimoLogin { get; set; }
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("PersonaId")]
    public Persona? Persona { get; set; }
    
    public ICollection<RolUsuario> RolesUsuario { get; set; } = new List<RolUsuario>();
}
