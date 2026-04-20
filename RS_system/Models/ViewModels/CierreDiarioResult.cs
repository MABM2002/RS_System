namespace Rs_system.Models.ViewModels;

public class CierreDiarioResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ColaboracionHeadId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalCerrado { get; set; }
    public long? ReporteMensualGeneralId { get; set; }
    public long? MovimientoGeneralId { get; set; }
    
    // For error details
    public string? Error { get; set; }
    public string? StackTrace { get; set; }
}