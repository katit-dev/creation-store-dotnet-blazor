using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Dashboard;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly CreationStoreDbContext _context;

        public AdminDashboardService(CreationStoreDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseTypeDTO<AdminDashboardSummaryDTO>>
            GetSummaryAsync()
        {
            var totalRevenue = await _context.Orders
                .AsNoTracking()
                .Where(o => o.PaymentStatus == CPaymentStatus.Succeeded)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                    // neu la null thi ep ve 0
                    // neu 
            var summary = new AdminDashboardSummaryDTO
            {
                TotalUsers = await _context.Users.CountAsync(),

                TotalProducts = await _context.Products.CountAsync(),

                TotalCategories = await _context.Categories.CountAsync(),

                TotalOrders = await _context.Orders.CountAsync(),

                TotalRevenue = totalRevenue,

                PendingPaymentOrders = await _context.Orders
                    .CountAsync(o => o.Status == COrderStatus.PendingPayment),

                PaidOrders = await _context.Orders
                    .CountAsync(o => o.Status == COrderStatus.Paid),

                CompletedOrders = await _context.Orders
                    .CountAsync(o => o.Status == COrderStatus.Completed),

                CancelledOrders = await _context.Orders
                    .CountAsync(o => o.Status == COrderStatus.Cancelled),

                TotalPayments = await _context.PaymentTransactions
                    .CountAsync(),

                PendingPayments = await _context.PaymentTransactions
                    .CountAsync(p =>
                        p.TransactionStatus == CPaymentTransactionStatus.Pending
                    ),

                SucceededPayments = await _context.PaymentTransactions
                    .CountAsync(p =>
                        p.TransactionStatus == CPaymentTransactionStatus.Succeeded
                    ),

                FailedPayments = await _context.PaymentTransactions
                    .CountAsync(p =>
                        p.TransactionStatus == CPaymentTransactionStatus.Failed
                    ),

                CancelledPayments = await _context.PaymentTransactions
                    .CountAsync(p =>
                        p.TransactionStatus == CPaymentTransactionStatus.Cancelled
                    )
            };

            return new ResponseTypeDTO<AdminDashboardSummaryDTO>
            {
                StatusCode = 200,
                Message = "Get dashboard summary successfully",
                Content = summary
            };
        }

        public async Task<ResponseTypeDTO<AdminRevenueStatisticDTO>>
            GetRevenueAsync(DateTime? fromDate, DateTime? toDate)
        {
            var startDate = fromDate?.Date ?? DateTime.Today.AddDays(-6);
            var endDate = toDate?.Date ?? DateTime.Today;

            if (startDate > endDate)
            {
                return new ResponseTypeDTO<AdminRevenueStatisticDTO>
                {
                    StatusCode = 400,
                    Message = "FromDate cannot be greater than ToDate",
                    Content = null
                };
            }

            var endDateExclusive = endDate.AddDays(1);

            var items = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.PaymentStatus == CPaymentStatus.Succeeded &&
                    o.OrderDate >= startDate &&
                    o.OrderDate < endDateExclusive
                )
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new AdminRevenueItemDTO
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var result = new AdminRevenueStatisticDTO
            {
                FromDate = startDate,
                ToDate = endDate,
                TotalRevenue = items.Sum(x => x.Revenue),
                Items = items
            };

            return new ResponseTypeDTO<AdminRevenueStatisticDTO>
            {
                StatusCode = 200,
                Message = "Get revenue statistic successfully",
                Content = result
            };
        }

        public async Task<ResponseTypeDTO<List<AdminTopProductDTO>>>
            GetTopProductsAsync(int take)
        {
            take = NormalizeTake(take, 5, 20);

            var result = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Where(oi =>
                    oi.Order.PaymentStatus == CPaymentStatus.Succeeded
                )
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.ProductName
                })
                .Select(g => new AdminTopProductDTO
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    SoldQuantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.PriceAtTime)
                })
                .OrderByDescending(x => x.SoldQuantity)
                .ThenByDescending(x => x.Revenue)
                .Take(take)
                .ToListAsync();

            return new ResponseTypeDTO<List<AdminTopProductDTO>>
            {
                StatusCode = 200,
                Message = "Get top products successfully",
                Content = result
            };
        }

        public async Task<ResponseTypeDTO<List<AdminRecentOrderDTO>>>
            GetRecentOrdersAsync(int take)
        {
            take = NormalizeTake(take, 10, 50);

            var result = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(take)
                .Select(o => new AdminRecentOrderDTO
                {
                    OrderId = o.OrderId,
                    UserId = o.UserId,
                    Username = o.User.Username,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            return new ResponseTypeDTO<List<AdminRecentOrderDTO>>
            {
                StatusCode = 200,
                Message = "Get recent orders successfully",
                Content = result
            };
        }

        private static int NormalizeTake(
            int take,
            int defaultTake,
            int maxTake
        )
        {
            if (take <= 0)
            {
                return defaultTake;
            }

            if (take > maxTake)
            {
                return maxTake;
            }

            return take;
        }
    }
}