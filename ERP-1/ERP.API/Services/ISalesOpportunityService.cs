using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.API.Models;
using ERP.API.Models.DTOs;

namespace ERP.API.Services
{
    public interface ISalesOpportunityService
    {
        Task<IEnumerable<SalesOpportunity>> GetOpportunitiesAsync();
        Task<SalesOpportunity?> GetOpportunityByIdAsync(string opportunityId);
        Task<SalesOpportunity?> GetByIdAsync(int id); // Gets opportunity by numeric database ID
        Task<IEnumerable<SalesOpportunityDto>> GetOpportunitiesByLeadIdAsync(int leadId);
        Task<int> CreateOpportunityAsync(SalesOpportunity opportunity);
        Task<bool> UpdateOpportunityAsync(string opportunityId, SalesOpportunity opportunity);
        Task<bool> DeleteOpportunityAsync(string opportunityId);
        Task<(IEnumerable<SalesOpportunityGridResult> Results, int TotalRecords)> GetOpportunitiesGridAsync(
            string? searchText = null,
            string[]? customerNames = null,
            string[]? territories = null,
            string[]? statuses = null,
            string[]? stages = null,
            string[]? opportunityTypes = null,
            int pageNumber = 1,
            int pageSize = 10,
            string? orderBy = "date_created",
            string? orderDirection = "DESC");

        /// <summary>
        /// Gets opportunity counts grouped by status for the cards view
        /// </summary>
        /// <returns>Collection of opportunity cards with status, count, and total value</returns>
        Task<IEnumerable<OpportunityCardDto>> GetOpportunityCardsAsync();
    }
}
