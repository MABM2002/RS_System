using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Los nombres son requeridos")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    [Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Los apellidos son requeridos")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Solo letras, números y guiones bajos")]
    [Display(Name = "Nombre de Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Debe confirmar la contraseña")]
    [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}
