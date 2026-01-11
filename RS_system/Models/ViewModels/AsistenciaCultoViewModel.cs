using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Rs_system.Models.Enums;

namespace Rs_system.Models.ViewModels;

public class AsistenciaCultoViewModel : IValidatableObject
{
    public long? Id { get; set; }
    
    [Required(ErrorMessage = "La fecha y hora de inicio es requerida")]
    [Display(Name = "Fecha y Hora de Inicio")]
    [DataType(DataType.DateTime)]
    public DateTime FechaHoraInicio { get; set; } = DateTime.Now;
    
    [Required(ErrorMessage = "El tipo de culto es requerido")]
    [Display(Name = "Tipo de Culto")]
    public TipoCulto TipoCulto { get; set; }
    
    [Required(ErrorMessage = "El tipo de conteo es requerido")]
    [Display(Name = "Tipo de Conteo")]
    public TipoConteo TipoConteo { get; set; }
    
    // Campos para TipoConteo.Detallado
    [Display(Name = "Hermanas (Concilio Misionero Femenil)")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? HermanasMisioneras { get; set; }
    
    [Display(Name = "Hermanos (Fraternidad de Varones)")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? HermanosFraternidad { get; set; }
    
    [Display(Name = "Embajadores de Cristo")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? EmbajadoresCristo { get; set; }
    
    [Display(Name = "Niños")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? Ninos { get; set; }
    
    [Display(Name = "Visitas")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? Visitas { get; set; }
    
    [Display(Name = "Amigos")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? Amigos { get; set; }
    
    // Campo para TipoConteo.General
    [Display(Name = "Adultos en General")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? AdultosGeneral { get; set; }
    
    // Campo para TipoConteo.Total
    [Display(Name = "Total Presente")]
    [Range(0, 10000, ErrorMessage = "El valor debe estar entre 0 y 10000")]
    public int? TotalManual { get; set; }
    
    [Display(Name = "Observaciones")]
    [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
    public string? Observaciones { get; set; }
    
    // Propiedades calculadas (solo lectura)
    [Display(Name = "Total Calculado")]
    [ReadOnly(true)]
    public int Total
    {
        get
        {
            return TipoConteo switch
            {
                TipoConteo.Detallado => (HermanasMisioneras ?? 0) + 
                                        (HermanosFraternidad ?? 0) + 
                                        (EmbajadoresCristo ?? 0) + 
                                        (Ninos ?? 0) + 
                                        (Visitas ?? 0) + 
                                        (Amigos ?? 0),
                TipoConteo.General => (AdultosGeneral ?? 0) + (Ninos ?? 0),
                TipoConteo.Total => TotalManual ?? 0,
                _ => 0
            };
        }
    }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        
        // Validar según TipoConteo
        switch (TipoConteo)
        {
            case TipoConteo.Detallado:
                // Todos los campos detallados son requeridos
                if (!HermanasMisioneras.HasValue)
                    results.Add(new ValidationResult("El campo 'Hermanas' es requerido para conteo detallado", new[] { nameof(HermanasMisioneras) }));
                else if (HermanasMisioneras.Value < 0)
                    results.Add(new ValidationResult("El campo 'Hermanas' debe ser mayor o igual a 0", new[] { nameof(HermanasMisioneras) }));
                
                if (!HermanosFraternidad.HasValue)
                    results.Add(new ValidationResult("El campo 'Hermanos' es requerido para conteo detallado", new[] { nameof(HermanosFraternidad) }));
                else if (HermanosFraternidad.Value < 0)
                    results.Add(new ValidationResult("El campo 'Hermanos' debe ser mayor o igual a 0", new[] { nameof(HermanosFraternidad) }));
                
                if (!EmbajadoresCristo.HasValue)
                    results.Add(new ValidationResult("El campo 'Embajadores' es requerido para conteo detallado", new[] { nameof(EmbajadoresCristo) }));
                else if (EmbajadoresCristo.Value < 0)
                    results.Add(new ValidationResult("El campo 'Embajadores' debe ser mayor o igual a 0", new[] { nameof(EmbajadoresCristo) }));
                
                if (!Ninos.HasValue)
                    results.Add(new ValidationResult("El campo 'Niños' es requerido para conteo detallado", new[] { nameof(Ninos) }));
                else if (Ninos.Value < 0)
                    results.Add(new ValidationResult("El campo 'Niños' debe ser mayor o igual a 0", new[] { nameof(Ninos) }));
                
                if (!Visitas.HasValue)
                    results.Add(new ValidationResult("El campo 'Visitas' es requerido para conteo detallado", new[] { nameof(Visitas) }));
                else if (Visitas.Value < 0)
                    results.Add(new ValidationResult("El campo 'Visitas' debe ser mayor o igual a 0", new[] { nameof(Visitas) }));
                
                if (!Amigos.HasValue)
                    results.Add(new ValidationResult("El campo 'Amigos' es requerido para conteo detallado", new[] { nameof(Amigos) }));
                else if (Amigos.Value < 0)
                    results.Add(new ValidationResult("El campo 'Amigos' debe ser mayor o igual a 0", new[] { nameof(Amigos) }));
                break;
                
            case TipoConteo.General:
                // AdultosGeneral y Ninos son requeridos
                if (!AdultosGeneral.HasValue)
                    results.Add(new ValidationResult("El campo 'Adultos en General' es requerido para conteo general", new[] { nameof(AdultosGeneral) }));
                else if (AdultosGeneral.Value < 0)
                    results.Add(new ValidationResult("El campo 'Adultos en General' debe ser mayor o igual a 0", new[] { nameof(AdultosGeneral) }));
                
                if (!Ninos.HasValue)
                    results.Add(new ValidationResult("El campo 'Niños' es requerido para conteo general", new[] { nameof(Ninos) }));
                else if (Ninos.Value < 0)
                    results.Add(new ValidationResult("El campo 'Niños' debe ser mayor o igual a 0", new[] { nameof(Ninos) }));
                break;
                
            case TipoConteo.Total:
                // Solo TotalManual es requerido
                if (!TotalManual.HasValue)
                    results.Add(new ValidationResult("El campo 'Total Presente' es requerido para conteo total", new[] { nameof(TotalManual) }));
                else if (TotalManual.Value < 0)
                    results.Add(new ValidationResult("El campo 'Total Presente' debe ser mayor o igual a 0", new[] { nameof(TotalManual) }));
                break;
        }
        
        return results;
    }
}