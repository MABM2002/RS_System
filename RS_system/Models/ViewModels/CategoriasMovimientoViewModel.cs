using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

public class CategoriasMovimientoViewModel
{
    public List<CategoriaIngreso> CategoriasIngreso { get; set; } = new();
    public List<CategoriaEgreso> CategoriasEgreso { get; set; } = new();
}

public class CategoriaIngresoViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres.")]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;

    [Display(Name = "Cuenta Contable")]
    public long? CuentaContableId { get; set; }
}

public class CategoriaEgresoViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres.")]
    public string? Descripcion { get; set; }

    public bool Activa { get; set; } = true;

    [Display(Name = "Cuenta Contable")]
    public long? CuentaContableId { get; set; }
}
