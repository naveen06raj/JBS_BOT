using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.API.Models;
using ERP.API.Models.DTOs;

namespace ERP.API.Services
{
    public interface ISalesDemoService
    {
        // Basic CRUD
        Task<IEnumerable<SalesDemo>> GetDemosAsync();
        Task<SalesDemo?> GetDemoByIdAsync(int id);
        Task<int> CreateDemoAsync(SalesDemo demo);
        Task<bool> UpdateDemoAsync(int id, SalesDemo demo);
        Task<bool> DeleteDemoAsync(int id);        // Opportunity related
        Task<IEnumerable<SalesDemo>> GetDemosByOpportunityIdAsync(int opportunityId);
        /// <summary>
        /// Gets demo cards count for dashboard
        /// </summary>
        /// <returns>DemoCardsDto with only counts</returns>
        Task<DemoCardsDto> GetDemoCardsAsync();
    }
}
