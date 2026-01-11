using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("roles_permisos")]
public class RolPermiso
{
    [Column("rol_id")]
    public int RolId { get; set; }

    [Column("permiso_id")]
    public int PermisoId { get; set; }

    [Column("asignado_en")]
    public DateTime AsignadoEn { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RolId")]
    public RolSistema Rol { get; set; } = null!;

    [ForeignKey("PermisoId")]
    public Permiso Permiso { get; set; } = null!;
}
