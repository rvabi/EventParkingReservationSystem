using EventParking.DataAccess.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventParking.DataAccess.Repositories;

internal sealed class RepositoryTransaction : IRepositoryTransaction
{
    private readonly IDbContextTransaction _transaction;

    public RepositoryTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException exception)
            when (exception.Number == 1205 ||
                  exception.Number == 1222)
        {
            throw new InvalidOperationException(
                "The transaction conflicted with another booking request. Refresh availability and try again.",
                exception);
        }
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}
