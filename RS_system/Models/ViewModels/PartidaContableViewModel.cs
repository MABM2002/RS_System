namespace Rs_system.Models.ViewModels;

/// <summary>
/// ViewModel para la creación de partidas contables (asientos de diario).
/// </summary>
public class PartidaContableViewModel
{
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string? Referencia { get; set; }
    public string? Descripcion { get; set; }
    public long? PeriodoContableId { get; set; }
    public List<DetallePartidaViewModel> Detalles { get; set; } = new()
    {
        new(), new() // Initial 2 empty lines
    };
}

public class DetallePartidaViewModel
{
    public long? CuentaContableId { get; set; }
    public decimal? Debito { get; set; }
    public decimal? Credito { get; set; }
    public string? Descripcion { get; set; }
}
