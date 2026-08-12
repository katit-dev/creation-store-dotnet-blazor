namespace CreationStore.Blazor.DTOs.Admin.Dashboard
{
    public class AdminDashboardSummaryDTO
    {
        public int TotalUsers { get; set; }

        public int TotalProducts { get; set; }

        public int TotalOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public int PendingPaymentOrders { get; set; }

        public int PaidOrders { get; set; }

        public int CompletedOrders { get; set; }

        public int CancelledOrders { get; set; }

        public int SucceededPayments { get; set; }

        public int FailedPayments { get; set; }
    }
}