using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

public class UsuarioViewModel
{
    public long? Id { get; set; }

    [Required(ErrorMessage = "Los nombres son requeridos")]
    [Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son requeridos")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [Display(Name = "Nombre de Usuario")]
    [StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Contraseña")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string? Contrasena { get; set; }

    [Display(Name = "Confirmar Contraseña")]
    [DataType(DataType.Password)]
    [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
    public string? ConfirmarContrasena { get; set; }

    [Display(Name = "Estado")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Roles Asignados")]
    public List<int> SelectedRoles { get; set; } = new List<int>();
}
