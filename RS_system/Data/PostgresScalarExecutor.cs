using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Rs_system.Data;

public static class PostgresScalarExecutor
{
    public static async Task<T> ExecuteAsync<T>(
        DbContext context,
        string sql,
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

        var result = await cmd.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException("La consulta escalar no devolvió ningún valor.");

        return (T)Convert.ChangeType(result, typeof(T));
    }
}