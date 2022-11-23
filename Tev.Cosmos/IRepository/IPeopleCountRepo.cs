using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos.IRepository
{
    public interface IPeopleCountRepo
    {
        Task<List<PeopleCount>> GetPeopleCount(int skip,int take,string deviceId,string orgId);
    }
}
