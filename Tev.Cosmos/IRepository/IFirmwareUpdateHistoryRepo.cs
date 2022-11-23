using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tev.Cosmos.Entity;

namespace Tev.Cosmos.IRepository
{
    public interface IFirmwareUpdateHistoryRepo
    {
        Task<List<FirmwareUpdateHistory>> GetFirmwareUpdateHisotry(string deviceId, string orgId);
    }
}
