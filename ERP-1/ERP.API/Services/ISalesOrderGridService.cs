using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.API.Models;

namespace ERP.API.Services
{
    public interface ISalesOrderGridService
    {
        Task<(IEnumerable<SalesOrderGrid> Data, int TotalRecords)> GetSalesOrderGridAsync(SalesOrderGridRequest request);
    }
}
