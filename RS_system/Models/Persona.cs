using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

[Table("personas")]
public class Persona
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("nombres")]
    [Required]
    [StringLength(100)]
    public string Nombres { get; set; } = string.Empty;
    
    [Column("apellidos")]
    [Required]
    [StringLength(100)]
    public string Apellidos { get; set; } = string.Empty;
    
    [Column("dui")]
    [StringLength(12)]
    public string? Dui { get; set; }
    
    [Column("nit")]
    [StringLength(17)]
    public string? Nit { get; set; }
    
    [Column("fecha_nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }
    
    [Column("genero")]
    [StringLength(1)]
    public string? Genero { get; set; }
    
    [Column("email")]
    [StringLength(255)]
    public string? Email { get; set; }
    
    [Column("telefono")]
    [StringLength(20)]
    public string? Telefono { get; set; }
    
    [Column("direccion")]
    public string? Direccion { get; set; }
    
    [Column("foto_url")]
    public string? FotoUrl { get; set; }
    
    [Column("activo")]
    public bool Activo { get; set; } = true;
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    
    // Nombre completo
    [NotMapped]
    public string NombreCompleto => $"{Nombres} {Apellidos}";
}
