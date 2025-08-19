using System;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace ERP.API.Helpers
{
    public static class IdGenerator
    {
        private const string LeadPrefix = "LEAD-";
        private const string OpportunityPrefix = "OPP";
        private const string DemoPrefix = "DM";
        private const int SequenceLength = 3;

        public static async Task<string> GenerateLeadId(IDbConnection connection)
        {
            // Get the current max lead_id that follows our format (LEAD- + 3 digits)
            const string sql = @"
                SELECT lead_id 
                FROM sales_leads 
                WHERE lead_id SIMILAR TO 'LEAD-[0-9]{3}'
                ORDER BY lead_id DESC 
                LIMIT 1";

            var lastId = await connection.QueryFirstOrDefaultAsync<string>(sql);
            
            int nextNumber;
            if (lastId == null)
            {
                nextNumber = 1;
            }
            else
            {
                // Extract the number part and increment
                if (int.TryParse(lastId.Substring(5), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
                else
                {
                    nextNumber = 1;
                }
            }

            // Format: LEAD- + 3 digits padded with zeros
            return $"{LeadPrefix}{nextNumber.ToString().PadLeft(SequenceLength, '0')}";
        }        public static async Task<string> GenerateOpportunityId(IDbConnection connection)
        {            try
            {
                // Use the database function to generate opportunity id
                const string sql = "SELECT generate_opportunity_id()";
                var generatedId = await connection.QuerySingleAsync<string>(sql);
                
                if (string.IsNullOrEmpty(generatedId))
                {
                    throw new InvalidOperationException("Failed to generate opportunity ID");
                }

                return generatedId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating opportunity ID: {ex.Message}", ex);
            }
        }

        public static async Task<string> GenerateDemoId(IDbConnection connection)
        {
            // Get the current max demo_id that follows our format (DM + 5 digits)
            const string sql = @"
                SELECT demo_id 
                FROM sales_demos 
                WHERE demo_id SIMILAR TO 'DM[0-9]{5}'
                ORDER BY demo_id DESC 
                LIMIT 1";

            var lastId = await connection.QueryFirstOrDefaultAsync<string>(sql);
            
            int nextNumber;
            if (lastId == null)
            {
                nextNumber = 1;
            }
            else
            {
                // Extract the number part and increment
                if (int.TryParse(lastId.Substring(2), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
                else
                {
                    nextNumber = 1;
                }
            }

            // Format: DM + 5 digits padded with zeros
            return $"{DemoPrefix}{nextNumber.ToString().PadLeft(SequenceLength, '0')}";
        }
    }
}
