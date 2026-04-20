using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace Rs_system.Data;

/// <summary>
/// Servicio para ejecutar consultas y procedimientos almacenados de PostgreSQL directamente,
/// sin depender de Entity Framework. Puede ser inyectado como servicio en cualquier parte de la aplicación.
/// </summary>
public interface IPostgresDirectExecutor
{
    /// <summary>
    /// Ejecuta un procedimiento almacenado con parámetros de entrada y salida
    /// </summary>
    Task ExecuteStoredProcedureAsync(string procedureName, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta un procedimiento almacenado con parámetros de entrada y salida, y devuelve los parámetros actualizados
    /// </summary>
    Task<NpgsqlParameter[]> ExecuteStoredProcedureWithOutputAsync(string procedureName, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta un procedimiento almacenado y devuelve un valor escalar
    /// </summary>
    Task<T> ExecuteStoredProcedureScalarAsync<T>(string procedureName, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta un procedimiento almacenado y devuelve un DataTable con los resultados
    /// </summary>
    Task<DataTable> ExecuteStoredProcedureDataTableAsync(string procedureName, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta un procedimiento almacenado y devuelve una lista de objetos mapeados
    /// </summary>
    Task<List<T>> ExecuteStoredProcedureListAsync<T>(string procedureName, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta una consulta SQL y devuelve un DataTable
    /// </summary>
    Task<DataTable> ExecuteQueryDataTableAsync(string sql, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta una consulta SQL y devuelve un valor escalar
    /// </summary>
    Task<T> ExecuteQueryScalarAsync<T>(string sql, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta una consulta SQL y devuelve una lista de objetos mapeados
    /// </summary>
    Task<List<T>> ExecuteQueryListAsync<T>(string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters);
    
    /// <summary>
    /// Ejecuta una consulta SQL que no devuelve resultados (INSERT, UPDATE, DELETE)
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, params NpgsqlParameter[] parameters);
}

public class PostgresDirectExecutor : IPostgresDirectExecutor
{
    private readonly string _connectionString;

    public PostgresDirectExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL") 
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'PostgreSQL' en la configuración.");
    }

    /// <summary>
    /// Constructor alternativo que acepta directamente la cadena de conexión
    /// </summary>
    public PostgresDirectExecutor(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task ExecuteStoredProcedureAsync(string procedureName, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        await command.ExecuteNonQueryAsync();
    }

    public async Task<NpgsqlParameter[]> ExecuteStoredProcedureWithOutputAsync(string procedureName, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        await command.ExecuteNonQueryAsync();
        
        // Los parámetros de salida ya están actualizados en el array original
        return parameters;
    }

    public async Task<T> ExecuteStoredProcedureScalarAsync<T>(string procedureName, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        var result = await command.ExecuteScalarAsync();
        
        if (result == null || result == DBNull.Value)
        {
            return default!;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<DataTable> ExecuteStoredProcedureDataTableAsync(string procedureName, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        await using var reader = await command.ExecuteReaderAsync();
        var dataTable = new DataTable();
        dataTable.Load(reader);
        
        return dataTable;
    }

    public async Task<List<T>> ExecuteStoredProcedureListAsync<T>(string procedureName, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        var results = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }
        
        return results;
    }

    public async Task<DataTable> ExecuteQueryDataTableAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        await using var reader = await command.ExecuteReaderAsync();
        var dataTable = new DataTable();
        dataTable.Load(reader);
        
        return dataTable;
    }

    public async Task<T> ExecuteQueryScalarAsync<T>(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        var result = await command.ExecuteScalarAsync();
        
        if (result == null || result == DBNull.Value)
        {
            return default!;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<List<T>> ExecuteQueryListAsync<T>(string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        var results = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }
        
        return results;
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }

        return await command.ExecuteNonQueryAsync();
    }
}