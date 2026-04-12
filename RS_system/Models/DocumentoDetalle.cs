using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models
{
    [Table("documentodetalle")]
    public class DocumentoDetalle
    {
        [Key]
        [Column("iddetalle")]
        public int IdDetalle { get; set; }

        [Required]
        [Column("iddocumento")]
        public int IdDocumento { get; set; }

        [Required]
        [StringLength(100)]
        [Column("columnasql")]
        public string ColumnaSql { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("aliasmarcador")]
        public string AliasMarcador { get; set; } = string.Empty;

        [ForeignKey("IdDocumento")]
        public Documento? Documento { get; set; }
    }
}
