using System.Collections.Generic;
using System.Threading.Tasks;
using DSex2.Models;
using Microsoft.AspNetCore.Http;

namespace DSex2.Repositories
{
    public interface IDataRepository
    {
        string GetLast(string id, string last);
        Task<bool> GetFile(string id, string file);
        Task<string> Info(DSinfo info);
    }
}