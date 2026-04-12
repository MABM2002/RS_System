using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models
{
    [Table("documentos")]
    public class Documento
    {
        [Key]
        [Column("iddocumento")]
        public int IdDocumento { get; set; }

        [Required]
        [StringLength(150)]
        [Column("nombrecomun")]
        public string NombreComun { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("rutaplantilla")]
        public string RutaPlantilla { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("tablafrom")]
        public string TablaFrom { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("condicionwhere")]
        public string? CondicionWhere { get; set; }

        // Relación 1 a N con DocumentoDetalle
        public ICollection<DocumentoDetalle> Detalles { get; set; } = new List<DocumentoDetalle>();
    }
}
