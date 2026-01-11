using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Rs_system.Models.Enums;

namespace Rs_system.Models.ViewModels;

public class AsistenciaCultoFiltroViewModel
{
    [Display(Name = "Fecha Desde")]
    [DataType(DataType.Date)]
    public DateTime? FechaDesde { get; set; }
    
    [Display(Name = "Fecha Hasta")]
    [DataType(DataType.Date)]
    public DateTime? FechaHasta { get; set; }
    
    [Display(Name = "Tipo de Culto")]
    public TipoCulto? TipoCulto { get; set; }
    
    [Display(Name = "Tipo de Conteo")]
    public TipoConteo? TipoConteo { get; set; }
    
    public IEnumerable<AsistenciaCulto>? Resultados { get; set; }
}