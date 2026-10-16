using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace be.Infrastructure.Persistence;
public static class CommerceTransaction
{
    // Database-owned lock works across API instances. Short local transactions only;
    // never hold it across a provider or SMTP call. Conservative for the current store size.
    public static async Task<IDbContextTransaction?> BeginAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
            return null; // Unit tests use InMemory; concurrency tests use SQL Server.
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = N'Amaara-Phase6-Commerce', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Commerce lock unavailable', 1;", ct);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
