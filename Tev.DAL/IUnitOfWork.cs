using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.DAL.Entities;
using Tev.DAL.RepoContract;

namespace Tev.DAL
{
    public interface IUnitOfWork
    {
        Task<int> Commit();
        void RollBack();
        IDbContextTransaction BeginTransaction();
    }
}
