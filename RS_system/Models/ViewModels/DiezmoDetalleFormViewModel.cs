using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

/// <summary>Formulario modal para agregar un diezmo de un miembro.</summary>
public class DiezmoDetalleFormViewModel
{
    [Required(ErrorMessage = "Seleccione un miembro.")]
    [Display(Name = "Miembro")]
    public long MiembroId { get; set; }

    [Required(ErrorMessage = "El monto entregado es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser mayor a 0.")]
    [Display(Name = "Monto entregado")]
    public decimal MontoEntregado { get; set; }

    [Required(ErrorMessage = "El monto del diezmo (neto) es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El diezmo debe ser mayor a 0.")]
    [Display(Name = "Diezmo (Neto)")]
    public decimal MontoNeto { get; set; }

    // Este campo ahora vendrá como solo-lectura desde el formulario 
    public decimal CambioEntregado { get; set; } = 0;

    [Display(Name = "Observaciones")]
    [StringLength(300)]
    public string? Observaciones { get; set; }
}
