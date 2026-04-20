using System.ComponentModel.DataAnnotations;
using Rs_system.Models;

namespace Rs_system.Models.ViewModels;

public class RegistrarColaboracionViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un miembro")]
    public long MiembroId { get; set; }
    
    [Display(Name = "Mes Inicial")]
    [Required(ErrorMessage = "Debe seleccionar el mes inicial")]
    [Range(1, 12, ErrorMessage = "Mes debe estar entre 1 y 12")]
    public int MesInicial { get; set; }
    
    [Display(Name = "Año Inicial")]
    [Required(ErrorMessage = "Debe seleccionar el año inicial")]
    [Range(2000, 2100, ErrorMessage = "Año debe estar entre 2000 y 2100")]
    public int AnioInicial { get; set; }
    
    [Display(Name = "Mes Final")]
    [Required(ErrorMessage = "Debe seleccionar el mes final")]
    [Range(1, 12, ErrorMessage = "Mes debe estar entre 1 y 12")]
    public int MesFinal { get; set; }
    
    [Display(Name = "Año Final")]
    [Required(ErrorMessage = "Debe seleccionar el año final")]
    [Range(2000, 2100, ErrorMessage = "Año debe estar entre 2000 y 2100")]
    public int AnioFinal { get; set; }
    
    [Required(ErrorMessage = "Debe seleccionar al menos un tipo de colaboración")]
    public List<long> TiposSeleccionados { get; set; } = new();
    
    [Required(ErrorMessage = "Debe ingresar el monto total")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto total debe ser mayor a 0")]
    [Display(Name = "Monto Total Entregado")]
    public decimal MontoTotal { get; set; }
    
    [Display(Name = "Tipo de Colaboración Prioritaria")]
    public long? TipoPrioritario { get; set; }
    
    [MaxLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
    
    public int IdJornada {get;set;}
    // Para cargar en el formulario
    
    public List<TipoColaboracion> TiposDisponibles { get; set; } = new();
    
    // Propiedad calculada: Total de meses
    public int TotalMeses
    {
        get
        {
            try
            {
                var fechaInicial = new DateTime(AnioInicial, MesInicial, 1);
                var fechaFinal = new DateTime(AnioFinal, MesFinal, 1);
                
                if (fechaFinal < fechaInicial)
                    return 0;
                
                return ((AnioFinal - AnioInicial) * 12) + (MesFinal - MesInicial) + 1;
            }
            catch
            {
                return 0;
            }
        }
    }
    
    // Propiedad calculada: Monto sugerido total basado en los tipos seleccionados
    public decimal MontoSugeridoTotal
    {
        get
        {
            if (TiposDisponibles == null || !TiposSeleccionados.Any())
                return 0;
            
            var tiposSeleccionadosData = TiposDisponibles
                .Where(t => TiposSeleccionados.Contains(t.Id))
                .ToList();
            
            var montoSugeridoPorMes = tiposSeleccionadosData.Sum(t => t.MontoSugerido);
            return montoSugeridoPorMes * TotalMeses;
        }
    }
}
