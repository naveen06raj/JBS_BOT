using System;

namespace ERP.API.Models.DTOs
{
    public class LeadCardsDto
    {
        public long TotalLeads { get; set; }
        public long NewThisWeek { get; set; }
        public long QualifiedLeads { get; set; }
        public long ConvertedLeads { get; set; }
        public long DemoScheduled { get; internal set; }
    }
}
