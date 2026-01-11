using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Rs_system.Data;

public static class PostgresQueryExecutor
{
    public static async Task<List<T>> ExecuteQueryAsync<T>(
        DbContext context,
        string sql,
        Func<NpgsqlDataReader, T> map,
        Action<NpgsqlParameterCollection>? parameters = null
    )
    {
        var conn = (NpgsqlConnection)context.Database.GetDbConnection();

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);

        var currentTx = context.Database.CurrentTransaction;
        if (currentTx != null)
            cmd.Transaction = (NpgsqlTransaction)currentTx.GetDbTransaction();

        parameters?.Invoke(cmd.Parameters);

        var results = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }
}
