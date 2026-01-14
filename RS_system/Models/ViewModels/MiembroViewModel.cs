using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Rs_system.Models.ViewModels;

public class MiembroViewModel
{
    public long Id { get; set; }
    
    [Required(ErrorMessage = "Los nombres son requeridos")]
    [StringLength(100, ErrorMessage = "Los nombres no pueden exceder 100 caracteres")]
    [Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Los apellidos son requeridos")]
    [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;
    
    [Display(Name = "Fecha de Nacimiento")]
    [DataType(DataType.Date)]
    public DateOnly? FechaNacimiento { get; set; }
    
    [Display(Name = "Bautizado en el Espíritu Santo")]
    public bool BautizadoEspirituSanto { get; set; } = false;
    
    [Display(Name = "Dirección")]
    [DataType(DataType.MultilineText)]
    public string? Direccion { get; set; }
    
    [Display(Name = "Fecha de Ingreso a la Congregación")]
    [DataType(DataType.Date)]
    public DateOnly? FechaIngresoCongregacion { get; set; }
    
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    [Display(Name = "Teléfono")]
    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    public string? Telefono { get; set; }
    
    [StringLength(20, ErrorMessage = "El teléfono de emergencia no puede exceder 20 caracteres")]
    [Display(Name = "Teléfono de Emergencia")]
    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    public string? TelefonoEmergencia { get; set; }
    
    [Display(Name = "Grupo de Trabajo")]
    public long? GrupoTrabajoId { get; set; }
    
    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
    
    [Display(Name = "Foto del Miembro")]
    public string? FotoUrl { get; set; }
    
    [Display(Name = "Subir Foto")]
    [DataType(DataType.Upload)]
    public IFormFile? FotoFile { get; set; }
    
    // For display purposes
    public string? GrupoTrabajoNombre { get; set; }
    public string NombreCompleto => $"{Nombres} {Apellidos}";
}
