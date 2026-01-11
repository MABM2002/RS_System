using Microsoft.EntityFrameworkCore;

namespace Rs_system.Data;

public static class DbContextOptimizationExtensions
{
    public static IQueryable<T> AsNoTrackingWithIdentityResolution<T>(this IQueryable<T> query) where T : class
    {
        return query.AsNoTrackingWithIdentityResolution();
    }

    public static IQueryable<T> AsSplitQuery<T>(this IQueryable<T> query) where T : class
    {
        return query.AsSplitQuery();
    }

    public static IQueryable<T> TagWith<T>(this IQueryable<T> query, string comment) where T : class
    {
        return query.TagWith(comment);
    }

    public static async Task<List<T>> ToListWithCountAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        var result = await query.ToListAsync(cancellationToken);
        return result;
    }

    public static async Task<T?> FirstOrDefaultNoTrackingAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default) where T : class
    {
        return await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<bool> AnyNoTrackingAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default) where T : class
    {
        return await query.AsNoTracking().AnyAsync(cancellationToken);
    }

    public static async Task<int> CountNoTrackingAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default) where T : class
    {
        return await query.AsNoTracking().CountAsync(cancellationToken);
    }
}