using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rs_system.Models.Enums;

namespace Rs_system.Models;

[Table("asistencias_culto")]
public class AsistenciaCulto
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("fecha_hora_inicio")]
    [Required]
    public DateTime FechaHoraInicio { get; set; }
    
    [Column("tipo_culto")]
    [Required]
    public TipoCulto TipoCulto { get; set; }
    
    [Column("tipo_conteo")]
    [Required]
    public TipoConteo TipoConteo { get; set; }
    
    // Campos para TipoConteo.Detallado
    [Column("hermanas_misioneras")]
    public int? HermanasMisioneras { get; set; }
    
    [Column("hermanos_fraternidad")]
    public int? HermanosFraternidad { get; set; }
    
    [Column("embajadores_cristo")]
    public int? EmbajadoresCristo { get; set; }
    
    [Column("ninos")]
    public int? Ninos { get; set; }
    
    [Column("visitas")]
    public int? Visitas { get; set; }
    
    [Column("amigos")]
    public int? Amigos { get; set; }
    
    // Campos para TipoConteo.General
    [Column("adultos_general")]
    public int? AdultosGeneral { get; set; }
    
    // Campo para TipoConteo.Total
    [Column("total_manual")]
    public int? TotalManual { get; set; }
    
    // Campos de auditoría
    [Column("observaciones")]
    [StringLength(500)]
    public string? Observaciones { get; set; }
    
    [Column("creado_por")]
    public string? CreadoPor { get; set; }
    
    [Column("creado_en")]
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    
    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
    
    [NotMapped]
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
    
    [NotMapped]
    public int TotalAdultosDetallado => (HermanasMisioneras ?? 0) + 
                                        (HermanosFraternidad ?? 0) + 
                                        (EmbajadoresCristo ?? 0) + 
                                        (Visitas ?? 0) + 
                                        (Amigos ?? 0);
}