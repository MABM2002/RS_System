using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;
    
    [Display(Name = "Recordarme")]
    public bool RecordarMe { get; set; }
}
