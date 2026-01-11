namespace Rs_system.Services;

public interface IConfiguracionService
{
    Task<string?> GetValorAsync(string clave);
    Task<string> GetValorOrDefaultAsync(string clave, string defaultValue);
}
