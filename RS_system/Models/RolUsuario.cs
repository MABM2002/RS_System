using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("roles_usuario")]
public class RolUsuario
{
    [Column("usuario_id")]
    public long UsuarioId { get; set; }
    
    [Column("rol_id")]
    public int RolId { get; set; }
    
    [Column("asignado_en")]
    public DateTime AsignadoEn { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UsuarioId")]
    public Usuario Usuario { get; set; } = null!;
    
    [ForeignKey("RolId")]
    public RolSistema Rol { get; set; } = null!;
}
