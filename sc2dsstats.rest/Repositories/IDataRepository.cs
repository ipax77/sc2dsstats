using sc2dsstats.lib.Models;
using sc2dsstats.rest.Models;
using System.Threading.Tasks;

namespace sc2dsstats.rest.Repositories
{
    public interface IDataRepository
    {
        Task<bool> GetAutoFile(string id, string file);
        string AutoInfo(DSinfo info);
        Task<bool> GetDBFile(string id, string myfile);
    }
}