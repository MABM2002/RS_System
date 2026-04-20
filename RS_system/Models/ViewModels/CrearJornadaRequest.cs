using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

public class CrearJornadaRequest
{
    [Required(ErrorMessage = "La fecha es requerida")]
    public DateTime Fecha { get; set; }
}