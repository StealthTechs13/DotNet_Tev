using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.DAL.Entities;
using Tev.DAL.RepoConcrete;
using Tev.DAL.RepoContract;

namespace Tev.DAL
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        public UnitOfWork(AppDbContext dbContext)
        {
            this._dbContext = dbContext;
        }
        
        public IDbContextTransaction BeginTransaction()
        {
            var transaction = _dbContext.Database.BeginTransaction();
            return transaction;
        }

        public async Task<int> Commit()
        {
           var result = await _dbContext.SaveChangesAsync().ConfigureAwait(false);
           return result;
        }

        public void RollBack()
        {
            _dbContext.Dispose();
        }
    }
}
