namespace Rs_system.Models.ViewModels;

public class ReporteColaboracionesViewModel
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal TotalRecaudado { get; set; }
    
    public List<DesglosePorTipo> DesglosePorTipos { get; set; } = new();
    public List<DetalleMovimiento> Movimientos { get; set; } = new();
}

public class DesglosePorTipo
{
    public string TipoNombre { get; set; } = string.Empty;
    public int CantidadMeses { get; set; }
    public decimal TotalRecaudado { get; set; }
}

public class DetalleMovimiento
{
    public long ColaboracionId { get; set; }
    public DateTime Fecha { get; set; }
    public string NombreMiembro { get; set; } = string.Empty;
    public string TiposColaboracion { get; set; } = string.Empty;
    public string PeriodoCubierto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
