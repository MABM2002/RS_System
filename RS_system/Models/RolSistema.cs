using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("roles_sistema")]
public class RolSistema
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("codigo")]
    [Required]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;
    
    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    
    [Column("descripcion")]
    public string? Descripcion { get; set; }
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    public ICollection<RolUsuario> RolesUsuario { get; set; } = new List<RolUsuario>();
    public ICollection<RolPermiso> RolesPermisos { get; set; } = new List<RolPermiso>();
}
