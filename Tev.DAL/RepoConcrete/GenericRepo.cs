using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;

namespace Tev.DAL.RepoConcrete
{
    public class GenericRepo<TEntity> : IGenericRepo<TEntity> where TEntity : Entity, new()
    {
     

        private readonly AppDbContext _context;
        public GenericRepo(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Save user selected streaming type in DB
        /// </summary>
        /// <returns></returns>
        public int SaveDeviceStreamingType()
        {
            return _context.SaveChanges();
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Add(TEntity entity)
        {
            if (entity == null)
            {
                throw new InvalidOperationException("Unable to add a null entity to the repository.");
            }
            entity.CreatedDate = GetEpochDate();
            this._context.Set<TEntity>().Add(entity);
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
            {
                throw new InvalidOperationException("Unable to add a null entity to the repository.");
            }
            entity.CreatedDate = GetEpochDate();
            var result  = await this._context.Set<TEntity>().AddAsync(entity).ConfigureAwait(false);
            return result.Entity;
        }

        public async Task AddRangeAsync(params TEntity[] entities)
        {
            if(entities != null)
            {
                foreach (var entity in entities)
                {
                    entity.CreatedDate = GetEpochDate();
                }
                await this._context.Set<TEntity>().AddRangeAsync(entities).ConfigureAwait(false);
            }
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    entity.CreatedDate = GetEpochDate();
                }
                await this._context.Set<TEntity>().AddRangeAsync(entities).ConfigureAwait(false);
            }
        }

        public void UpdateRange(params TEntity[] entities)
        {
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    entity.ModifiedDate = GetEpochDate();
                }
                this._context.Set<TEntity>().UpdateRange(entities);
            }
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    entity.ModifiedDate = GetEpochDate();
                }
                this._context.Set<TEntity>().UpdateRange(entities);
            }
        }

        public TEntity Update(TEntity entity)
        {
            if (entity != null)
            {
                entity.ModifiedDate = GetEpochDate();
                return this._context.Set<TEntity>().Update(entity).Entity;
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        public void Remove(TEntity entity)
        {
            this._context.Set<TEntity>().Attach(entity);
            this._context.Entry(entity).State = EntityState.Deleted;
            this._context.Set<TEntity>().Remove(entity);
        }

        public void RemoveRangeAsync(params TEntity[] entities)
        {
            this._context.Set<TEntity>().RemoveRange(entities);
        }

        public void Remove(int id)
        {
            var entity = new TEntity();
            PropertyInfo propertyInfo = entity.GetType().GetProperty("Id");
            propertyInfo.SetValue(entity, Convert.ChangeType(id, propertyInfo.PropertyType), null);

            this.Remove(entity);
        }

        public IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> filter, bool AsNoTrack = false)
        {
            var result = QueryDb(filter, AsNoTrack);
            return result;
        }

        protected IQueryable<TEntity> QueryDb(Expression<Func<TEntity, bool>> filter, bool AsNoTrack = false)
        {
            IQueryable<TEntity> query = this._context.Set<TEntity>();

            if (AsNoTrack)
            {
                query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }


        public IQueryable<TEntity> GetAll()
        {
            var result = QueryDb(null);
            return result;
        }

        public long GetEpochDate()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var epochDate = (long)t.TotalSeconds;
            return epochDate;
        }

    }
}
