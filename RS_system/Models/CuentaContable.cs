using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rs_system.Models;

/// <summary>
/// Catálogo de Cuentas Contables — estructura jerárquica (padre-hijo).
/// El código se almacena sin puntos (ej: "1101") y se formatea con puntos en la UI (ej: "1.1.01").
/// </summary>
[Table("cuentas_contables")]
public class CuentaContable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Código jerárquico sin puntos (ej. "1101", "402"). El formato con puntos se genera vía CodigoFormateado.</summary>
    [Column("codigo")]
    [Required]
    [StringLength(20)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo de la cuenta.</summary>
    [Column("nombre")]
    [Required]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>ID de la cuenta padre (null si es raíz). Self-referencing FK.</summary>
    [Column("padre_id")]
    public long? PadreId { get; set; }

    [ForeignKey("PadreId")]
    public virtual CuentaContable? Padre { get; set; }

    /// <summary>FK al tipo de cuenta dinámico (AccountType).</summary>
    [Column("account_type_id")]
    [Required]
    public int AccountTypeId { get; set; }

    [ForeignKey("AccountTypeId")]
    public virtual AccountType AccountType { get; set; } = null!;

    /// <summary>Indica si la cuenta está activa para uso.</summary>
    [Column("activa")]
    public bool Activa { get; set; } = true;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CuentaContable> Hijas { get; set; } = new List<CuentaContable>();
    public virtual ICollection<DetallePartidaContable> DetallesPartida { get; set; } = new List<DetallePartidaContable>();
    public virtual ICollection<CategoriaIngreso> CategoriasIngreso { get; set; } = new List<CategoriaIngreso>();
    public virtual ICollection<CategoriaEgreso> CategoriasEgreso { get; set; } = new List<CategoriaEgreso>();

    // ==================== Formateo dinámico del código ====================

    /// <summary>
    /// Versión formateada del código para mostrar en UI.
    /// Inserta puntos según el nivel jerárquico de la cuenta.
    /// Ej: "1101" → "1.1.01" (profundidad 2), "11" → "1.1" (profundidad 1), "1" → "1" (raíz).
    /// </summary>
    [NotMapped]
    public string CodigoFormateado
    {
        get
        {
            if (string.IsNullOrEmpty(Codigo)) return Codigo;

            var profundidad = 0;
            var p = Padre;
            while (p != null) { profundidad++; p = p.Padre; }

            if (profundidad == 0 || Codigo.Length <= profundidad)
                return Codigo;

            var sb = new StringBuilder();
            for (int i = 0; i < profundidad; i++)
            {
                if (i >= Codigo.Length) break;
                sb.Append(Codigo[i]);
                sb.Append('.');
            }
            if (profundidad < Codigo.Length)
                sb.Append(Codigo.AsSpan(profundidad));

            return sb.ToString();
        }
    }
}
