using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERP.API.Models.DTOs;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ERP.API.Services
{
    public partial class SalesLeadService
    {
        public async Task<LeadCardsDto> GetLeadCardsAsync()
        {
            try
            {
                const string sql = "SELECT * FROM sp_get_lead_cards_count()";
                using var connection = CreateConnection();
                var cards = await connection.QueryFirstOrDefaultAsync<LeadCardsDto>(sql);
                  return cards ?? new LeadCardsDto();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting lead cards data: {ex.Message}", ex);
            }
        }
    }
}
