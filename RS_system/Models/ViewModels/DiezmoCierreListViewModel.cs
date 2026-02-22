using System.ComponentModel.DataAnnotations;

namespace Rs_system.Models.ViewModels;

/// <summary>Fila del listado de cierres de diezmos.</summary>
public class DiezmoCierreListViewModel
{
    public long    Id             { get; set; }
    public DateOnly Fecha         { get; set; }
    public bool    Cerrado        { get; set; }
    public decimal TotalRecibido  { get; set; }
    public decimal TotalNeto      { get; set; }
    public decimal TotalSalidas   { get; set; }
    public decimal SaldoFinal     { get; set; }
    public int     NumeroDetalles { get; set; }
    public int     NumeroSalidas  { get; set; }
    public string  EstadoBadge    => Cerrado ? "badge bg-secondary" : "badge bg-success";
    public string  EstadoTexto    => Cerrado ? "Cerrado" : "Abierto";
}
