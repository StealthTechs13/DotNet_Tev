using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tev.DAL.Entities;

namespace Tev.DAL.RepoContract
{
    public interface IGenericRepo<TEntity> where TEntity:Entity
    {
        void Add(TEntity entity);
        int SaveChanges();
        Task<TEntity> AddAsync(TEntity entity);
        Task AddRangeAsync(params TEntity[] entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        void UpdateRange(params TEntity[] entities);
        void UpdateRange(IEnumerable<TEntity> entities);
        TEntity Update(TEntity entity);
        void Remove(TEntity entity);
        void RemoveRangeAsync(params TEntity[] entities);
        void Remove(int id);
        IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> filter, bool AsNoTrack = false);
        IQueryable<TEntity> GetAll();

        /// <summary>
        /// Objective of this method is to save device streaming type for specific user. 
        /// </summary>
        /// <returns></returns>
        int SaveDeviceStreamingType();
    }
}
