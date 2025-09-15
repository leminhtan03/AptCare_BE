using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AptCare.Repository.UnitOfWork;

public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
{
    public TContext Context { get; }
    private Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(TContext context)
    {
        Context = context;
    }

    public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        _repositories ??= new Dictionary<Type, object>();
        if (_repositories.TryGetValue(typeof(TEntity), out object repository))
        {
            return (IGenericRepository<TEntity>)repository;
        }


        repository = new GenericRepository<TEntity>(Context);
        _repositories.Add(typeof(TEntity), repository);
        return (IGenericRepository<TEntity>)repository;
    }

    public void Dispose()
    {
        Context?.Dispose();
    }

    public int Commit()
    {
        TrackChanges();
        return Context.SaveChanges();
    }

    public async Task<int> CommitAsync()
    {
        TrackChanges();
        return await Context.SaveChangesAsync();
    }

    private void TrackChanges()
    {
        var validationErrors = Context.ChangeTracker.Entries<IValidatableObject>()
            .SelectMany(e => e.Entity.Validate(null))
            .Where(e => e != ValidationResult.Success)
            .ToArray();
        if (validationErrors.Any())
        {
            var exceptionMessage = string.Join(Environment.NewLine,
                validationErrors.Select(error => $"Properties {error.MemberNames} Error: {error.ErrorMessage}"));
            throw new Exception(exceptionMessage);
        }
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await Context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}