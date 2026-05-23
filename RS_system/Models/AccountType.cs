using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rs_system.Models;

/// <summary>
/// Tipo de cuenta contable. Define el nombre, naturaleza (deudora/acreedora)
/// y la categoría de reporte (Balance o Resultado).
/// Reemplaza al antiguo enum TipoCuenta — ahora es dinámico y administrable vía CRUD.
/// </summary>
[Table("tipos_cuenta_contable")]
public class AccountType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>Nombre del tipo de cuenta (ej: "Activo", "Pasivo", "Ingreso Financiero").</summary>
    [Column("nombre")]
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Naturaleza contable: Deudora (saldo natural débito) o Acreedora (saldo natural crédito).</summary>
    [Column("naturaleza")]
    [Required]
    public NaturalezaCuenta Naturaleza { get; set; }

    /// <summary>Categoría del reporte: Balance (Balance General) o Resultado (Estado de Resultados).</summary>
    [Column("categoria_reporte")]
    [Required]
    public CategoriaReporte CategoriaReporte { get; set; }

    /// <summary>Orden de presentación en listados y reportes.</summary>
    [Column("orden")]
    public int Orden { get; set; }

    /// <summary>Indica si el tipo está activo para uso.</summary>
    [Column("activo")]
    public bool Activo { get; set; } = true;

    // Navigation
    public virtual ICollection<CuentaContable> Cuentas { get; set; } = new List<CuentaContable>();
}
