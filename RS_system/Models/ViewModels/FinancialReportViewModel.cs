using Rs_system.Models;

namespace Rs_system.Models.ViewModels;

public class FinancialReportViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public List<ReportSectionViewModel> Secciones { get; set; } = new();
    
    // Para Balance General: Total Activo - (Total Pasivo + Total Patrimonio) debe ser 0
    public decimal DiferenciaBalance { get; set; }
}

public class ReportSectionViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public NaturalezaCuenta Naturaleza { get; set; }
    public int Orden { get; set; }
    public decimal Total { get; set; }
    public List<AccountReportItemViewModel> CuentasRaiz { get; set; } = new();
}

public class AccountReportItemViewModel
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string CodigoFormateado { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
    public int Nivel { get; set; }
    public List<AccountReportItemViewModel> SubCuentas { get; set; } = new();
    
    public bool TieneSubCuentas => SubCuentas.Any();
}
