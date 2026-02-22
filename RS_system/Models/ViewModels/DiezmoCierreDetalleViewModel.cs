using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
// Formulario — Nuevo cierre
// ─────────────────────────────────────────────────────────────────────────────
public class DiezmoCierreCreateViewModel
{
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [Display(Name = "Fecha del cierre")]
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Observaciones")]
    [StringLength(500)]
    public string? Observaciones { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Pantalla operativa de detalle del cierre
// ─────────────────────────────────────────────────────────────────────────────
public class DiezmoCierreDetalleViewModel
{
    public long     Id            { get; set; }
    public DateOnly Fecha         { get; set; }
    public bool     Cerrado       { get; set; }
    public string?  Observaciones { get; set; }
    public string?  CerradoPor    { get; set; }
    public DateTime? FechaCierre  { get; set; }

    // Totales
    public decimal TotalRecibido { get; set; }
    public decimal TotalCambio   { get; set; }
    public decimal TotalNeto     { get; set; }
    public decimal TotalSalidas  { get; set; }
    public decimal SaldoFinal    { get; set; }

    // Datos de detalles
    public List<DiezmoDetalleRowViewModel> Detalles { get; set; } = new();

    // Datos de salidas
    public List<DiezmoSalidaRowViewModel> Salidas { get; set; } = new();

    // Formularios embebidos para modales
    public DiezmoDetalleFormViewModel FormDetalle { get; set; } = new();
    public DiezmoSalidaFormViewModel  FormSalida  { get; set; } = new();

    // Datos de selectores para los modales
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> MiembrosSelect     { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> TiposSalidaSelect  { get; set; } = new();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> BeneficiariosSelect { get; set; } = new();

    public string EstadoBadge => Cerrado ? "badge bg-secondary" : "badge bg-success";
    public string EstadoTexto => Cerrado ? "Cerrado" : "Abierto";
}

// ─────────────────────────────────────────────────────────────────────────────
// Fila de un detalle en la tabla
// ─────────────────────────────────────────────────────────────────────────────
public class DiezmoDetalleRowViewModel
{
    public long    Id              { get; set; }
    public long    MiembroId       { get; set; }
    public string  NombreMiembro   { get; set; } = string.Empty;
    public decimal MontoEntregado  { get; set; }
    public decimal CambioEntregado { get; set; }
    public decimal MontoNeto       { get; set; }
    public string? Observaciones   { get; set; }
    public DateTime Fecha          { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Fila de una salida en la tabla
// ─────────────────────────────────────────────────────────────────────────────
public class DiezmoSalidaRowViewModel
{
    public long    Id              { get; set; }
    public string  TipoSalidaNombre { get; set; } = string.Empty;
    public string? BeneficiarioNombre { get; set; }
    public decimal Monto           { get; set; }
    public string  Concepto        { get; set; } = string.Empty;
    public string? NumeroRecibo    { get; set; }
    public DateTime Fecha          { get; set; }
}
