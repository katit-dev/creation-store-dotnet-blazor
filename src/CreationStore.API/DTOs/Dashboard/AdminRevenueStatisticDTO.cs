namespace CreationStore.API.DTOs.Admin.Dashboard
{
    public class AdminRevenueStatisticDTO
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<AdminRevenueItemDTO> Items { get; set; } = new();
    }
}