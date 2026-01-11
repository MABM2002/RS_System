using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

/// <summary>
/// ViewModel for creating/editing offering records
/// </summary>
public class RegistroCultoViewModel
{
    public long? Id { get; set; }
    
    [Required(ErrorMessage = "La fecha es requerida")]
    [Display(Name = "Fecha del Culto")]
    [DataType(DataType.Date)]
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    
    [Display(Name = "Observaciones")]
    [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
    public string? Observaciones { get; set; }
    
    public List<OfrendaItemViewModel> Ofrendas { get; set; } = new();
    
    // Calculated properties for display
    public decimal TotalOfrendas => Ofrendas?.Sum(o => o.Monto) ?? 0;
    public decimal TotalDescuentos => Ofrendas?.Sum(o => o.TotalDescuentos) ?? 0;
    public decimal MontoNeto => TotalOfrendas - TotalDescuentos;
}

/// <summary>
/// ViewModel for an individual offering
/// </summary>
public class OfrendaItemViewModel
{
    public long? Id { get; set; }
    
    [Required(ErrorMessage = "El monto es requerido")]
    [Display(Name = "Monto")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
    [DataType(DataType.Currency)]
    public decimal Monto { get; set; }
    
    [Required(ErrorMessage = "El concepto es requerido")]
    [Display(Name = "Concepto")]
    [StringLength(200, ErrorMessage = "El concepto no puede exceder 200 caracteres")]
    public string Concepto { get; set; } = string.Empty;
    
    public List<DescuentoItemViewModel> Descuentos { get; set; } = new();
    
    // Calculated properties
    public decimal TotalDescuentos => Descuentos?.Sum(d => d.Monto) ?? 0;
    public decimal MontoNeto => Monto - TotalDescuentos;
}

/// <summary>
/// ViewModel for a deduction from an offering
/// </summary>
public class DescuentoItemViewModel
{
    public long? Id { get; set; }
    
    [Required(ErrorMessage = "El monto es requerido")]
    [Display(Name = "Monto")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
    [DataType(DataType.Currency)]
    public decimal Monto { get; set; }
    
    [Required(ErrorMessage = "El concepto es requerido")]
    [Display(Name = "Concepto")]
    [StringLength(200, ErrorMessage = "El concepto no puede exceder 200 caracteres")]
    public string Concepto { get; set; } = string.Empty;
}
