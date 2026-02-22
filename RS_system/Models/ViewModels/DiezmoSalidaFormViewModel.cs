using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

/// <summary>Formulario modal para registrar una salida/entrega de fondos.</summary>
public class DiezmoSalidaFormViewModel
{
    [Required(ErrorMessage = "Seleccione el tipo de salida.")]
    [Display(Name = "Tipo de salida")]
    public long TipoSalidaId { get; set; }

    [Display(Name = "Beneficiario")]
    public long? BeneficiarioId { get; set; }

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser mayor a 0.")]
    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio.")]
    [StringLength(300)]
    [Display(Name = "Concepto")]
    public string Concepto { get; set; } = string.Empty;
}
