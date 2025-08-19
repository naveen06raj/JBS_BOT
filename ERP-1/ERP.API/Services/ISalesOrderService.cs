using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.API.Models;
using ERP.API.Models.DTOs;

namespace ERP.API.Services
{
    public interface ISalesOrderService
    {
        Task<IEnumerable<SalesOrderGrid>> GetAllSalesOrdersAsync();
        Task<SalesOrder> GetSalesOrderByIdAsync(int id);
        Task<int> CreateSalesOrderAsync(SalesOrder salesOrder);
        Task<SalesOrder> UpdateSalesOrderAsync(SalesOrder salesOrder);
        Task<bool> DeleteSalesOrderAsync(int id);
        Task<QuotationWithOrderResponse> GetQuotationByIdAsync(int id);
    }
}