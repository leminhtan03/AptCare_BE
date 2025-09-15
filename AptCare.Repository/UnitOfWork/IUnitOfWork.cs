using Microsoft.EntityFrameworkCore;
using AptCare.Repository;


namespace AptCare.Repository.UnitOfWork;
public interface IUnitOfWork : IGenericRepositoryFactory, IDisposable
{
    int Commit();

    Task<int> CommitAsync();

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}

public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    TContext Context { get; }
}

