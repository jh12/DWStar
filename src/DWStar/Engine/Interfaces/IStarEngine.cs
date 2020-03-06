using System.Collections.Generic;
using System.Threading.Tasks;
using DWStar.DataAccess.Models;

namespace DWStar.Engine.Interfaces
{
    public interface IStarEngine
    {
        Task<object> GetMetadata(IEnumerable<ConnectionInfo> connectionInfos);
    }
}