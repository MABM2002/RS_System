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
}
