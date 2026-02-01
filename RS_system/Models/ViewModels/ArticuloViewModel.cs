using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Rs_system.Models;

namespace Rs_system.Models.ViewModels;

public class ArticuloViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
    public string? Descripcion { get; set; }

    [StringLength(100)]
    public string? Marca { get; set; }

    [StringLength(100)]
    public string? Modelo { get; set; }

    [Display(Name = "Número de Serie")]
    [StringLength(100)]
    public string? NumeroSerie { get; set; }

    [Range(0, 99999999.99)]
    public decimal Precio { get; set; } = 0;

    [Display(Name = "Fecha de Adquisición")]
    public DateOnly? FechaAdquisicion { get; set; }

    public string? ImagenUrl { get; set; }

    [Display(Name = "Imagen")]
    public IFormFile? ImagenFile { get; set; }

    [Display(Name = "Tipo de Control")]
    public string? TipoControl { get; set; } = "UNITARIO"; // Default for View

    [Display(Name = "Cantidad Inicial")]
    [Range(1, 100000)]
    public int CantidadInicial { get; set; } = 1;

    public int CategoriaId { get; set; }

    [Display(Name = "Estado")]
    [Required(ErrorMessage = "El estado es obligatorio")]
    public int EstadoId { get; set; }

    [Display(Name = "Ubicación")]
    [Required(ErrorMessage = "La ubicación es obligatoria")]
    public int UbicacionId { get; set; }

    public bool Activo { get; set; } = true;
    
    // Display properties for lists/details
    public string? CategoriaNombre { get; set; }
    public string? EstadoNombre { get; set; }
    public string? EstadoColor { get; set; }
    public string? UbicacionNombre { get; set; }
    public int CantidadGlobal { get; set; }
}
