using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("configuracion_sistema")]
public class ConfiguracionSistema
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("clave")]
    [StringLength(100)]
    public string Clave { get; set; } = string.Empty;

    [Column("valor")]
    public string? Valor { get; set; }

    [Column("tipo_dato")]
    [StringLength(20)]
    public string TipoDato { get; set; } = "TEXTO";

    [Column("categoria")]
    [StringLength(50)]
    public string Categoria { get; set; } = "GENERAL";

    [Column("grupo")]
    [StringLength(50)]
    public string Grupo { get; set; } = "SISTEMA";

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("es_editable")]
    public bool EsEditable { get; set; } = true;

    [Column("es_publico")]
    public bool EsPublico { get; set; } = false;

    [Column("orden")]
    public int Orden { get; set; } = 0;

    [Column("opciones", TypeName = "jsonb")]
    public string? Opciones { get; set; }

    [Column("validacion_regex")]
    [StringLength(200)]
    public string? ValidacionRegex { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
