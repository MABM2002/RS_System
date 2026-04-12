using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels.Catalogos;

public class TipoSalidaViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [Display(Name = "Es Entrega Directa a Pastor")]
    public bool EsEntregaPastor { get; set; }
}

public class BeneficiarioViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    public long? IdPersona { get; set; }

    [StringLength(100)]
    public string? Nombres { get; set; }

    [StringLength(100)]
    public string? Apellidos { get; set; }

    [StringLength(12)]
    public string? Dui { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }
}
