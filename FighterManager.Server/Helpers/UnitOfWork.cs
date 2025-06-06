﻿using Microsoft.EntityFrameworkCore.Storage;
using SharedEntities.Data;

namespace FighterManager.Server.Helpers
{
    public interface IUnitOfWork : IDisposable
    {
        MyDatabaseContext AppDbContext { get; }

        IRepository<TEntity> Repository<TEntity>() where TEntity : class;
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }

    public class UnitOfWork(MyDatabaseContext context) : IUnitOfWork
    {
        private readonly MyDatabaseContext _context = context;
        private readonly Dictionary<Type, object> _repositories = [];

        public MyDatabaseContext AppDbContext => _context;

        public IRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            if (_repositories.ContainsKey(typeof(TEntity)))
            {
                return _repositories[typeof(TEntity)] as IRepository<TEntity>;
            }

            var repository = new Repository<TEntity>(_context);
            _repositories.Add(typeof(TEntity), repository);
            return repository;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
