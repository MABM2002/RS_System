namespace Rs_system.Models.ViewModels;

public class UltimoPagoViewModel
{
    public long TipoId { get; set; }
    public string NombreTipo { get; set; }
    public int UltimoMes { get; set; }
    public int UltimoAnio { get; set; }
    public DateTime FechaUltimoPago { get; set; }
    
    public string UltimoPeriodoTexto
    {
        get
        {
            if (UltimoMes == 0 || UltimoAnio == 0) return "Sin pagos registrados";
            return $"{ObtenerNombreMes(UltimoMes)} {UltimoAnio}";
        }
    }
    
    private string ObtenerNombreMes(int mes)
    {
        return mes switch
        {
            1 => "Enero",
            2 => "Febrero",
            3 => "Marzo",
            4 => "Abril",
            5 => "Mayo",
            6 => "Junio",
            7 => "Julio",
            8 => "Agosto",
            9 => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _ => ""
        };
    }
}
