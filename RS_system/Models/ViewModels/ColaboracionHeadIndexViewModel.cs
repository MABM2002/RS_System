using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

public class ColaboracionHeadIndexViewModel
{
    public long Id { get; set; }
    
    [Display(Name = "Fecha")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime Fecha { get; set; }
    
    [Display(Name = "Total")]
    [DisplayFormat(DataFormatString = "${0:N2}")]
    public decimal Total { get; set; }
    
    [Display(Name = "Cantidad de Colaboraciones")]
    public int CantidadColaboraciones { get; set; }
    
    [Display(Name = "Creado por")]
    public string? CreadoPor { get; set; }
    
    [Display(Name = "Creado en")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
    public DateTime CreadoEn { get; set; }
    
    [Display(Name = "Estado")]
    public bool EsCerrado { get; set; }
    
    [Display(Name = "Fecha Cierre")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
    public DateTime? FechaCierre { get; set; }
    
    [Display(Name = "Cerrado por")]
    public string? CerradoPor { get; set; }
}

public class ColaboracionHeadDetalleViewModel
{
    public long Id { get; set; }
    
    [Display(Name = "Fecha")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime Fecha { get; set; }
    
    [Display(Name = "Total")]
    [DisplayFormat(DataFormatString = "${0:N2}")]
    public decimal Total { get; set; }
    
    [Display(Name = "Creado por")]
    public string? CreadoPor { get; set; }
    
    [Display(Name = "Creado en")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
    public DateTime CreadoEn { get; set; }
    
    [Display(Name = "Estado")]
    public bool EsCerrado { get; set; }
    
    [Display(Name = "Fecha Cierre")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
    public DateTime? FechaCierre { get; set; }
    
    [Display(Name = "Cerrado por")]
    public string? CerradoPor { get; set; }
    
    public List<ColaboracionDetalleViewModel> Colaboraciones { get; set; } = new();
}

public class ColaboracionDetalleViewModel
{
    public long Id { get; set; }
    
    [Display(Name = "Miembro")]
    public string MiembroNombre { get; set; } = string.Empty;
    
    [Display(Name = "Monto")]
    [DisplayFormat(DataFormatString = "${0:N2}")]
    public decimal MontoTotal { get; set; }
    
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
    
    [Display(Name = "Registrado por")]
    public string? RegistradoPor { get; set; }
    
    [Display(Name = "Fecha Registro")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
    public DateTime FechaRegistro { get; set; }
    
    public List<string> TiposColaboracion { get; set; } = new();
}