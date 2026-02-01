namespace Rs_system.Models.ViewModels;

public class EstadoCuentaViewModel
{
    public long MiembroId { get; set; }
    public string NombreMiembro { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
    
    public List<HistorialPorTipo> HistorialPorTipos { get; set; } = new();
    public decimal TotalAportado { get; set; }
}

public class HistorialPorTipo
{
    public string TipoNombre { get; set; } = string.Empty;
    public List<RegistroMensual> Registros { get; set; } = new();
    public decimal TotalTipo { get; set; }
}

public class RegistroMensual
{
    public int Mes { get; set; }
    public int Anio { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaRegistro { get; set; }
    
    public string MesTexto => ObtenerMesTexto();
    
    private string ObtenerMesTexto()
    {
        try
        {
            var fecha = new DateTime(Anio, Mes, 1);
            return fecha.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
        }
        catch
        {
            return $"{Mes}/{Anio}";
        }
    }
}
