namespace CreationStore.Blazor.DTOs.Admin.Dashboard
{
    public class AdminRevenueDTO
    {
        public DateTime Date { get; set; }

        public DateTime RevenueDate { get; set; }

        public decimal Revenue { get; set; }

        public decimal TotalRevenue { get; set; }

        public int OrderCount { get; set; }

        public int TotalOrders { get; set; }

        public DateTime DisplayDate =>
            RevenueDate != default ? RevenueDate : Date;

        public decimal DisplayRevenue =>
            TotalRevenue > 0 ? TotalRevenue : Revenue;

        public int DisplayOrderCount =>
            TotalOrders > 0 ? TotalOrders : OrderCount;
    }
}