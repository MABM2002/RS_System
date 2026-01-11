using Microsoft.EntityFrameworkCore;
using Rs_system.Data;

namespace Rs_system.Services;

public class ConfiguracionService : IConfiguracionService
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetValorAsync(string clave)
    {
        var config = await _context.Configuraciones
            .FirstOrDefaultAsync(c => c.Clave == clave);
        return config?.Valor;
    }

    public async Task<string> GetValorOrDefaultAsync(string clave, string defaultValue)
    {
        var valor = await GetValorAsync(clave);
        return valor ?? defaultValue;
    }
}
