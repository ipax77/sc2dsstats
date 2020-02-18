using sc2dsstats.rest.Models;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Repositories
{
    public interface IDataRepository
    {
        string GetLast(string id, string last);
        Task<bool> GetFile(string id, string file);
        Task<bool> GetAutoFile(string id, string file);
        Task<bool> FullSend(string id, string file);
        Task<string> Info(DSinfo info);
        Task<string> AutoInfo(DSinfo info);
    }
}