namespace CreationStore.Blazor.DTOs.Admin.Dashboard
{
    public class AdminRevenueReportDTO
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<AdminRevenueDTO> Items { get; set; } = new();
    }
}