using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Orders;
using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.Payment;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class AdminOrderService : IAdminOrderService
    {
        private readonly CreationStoreDbContext _context;

        public AdminOrderService(CreationStoreDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseTypeDTO<List<AdminOrderResponseDTO>>>
            GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.PaymentTransactions)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = orders
                .Select(BuildAdminOrderResponse)
                .ToList();

            return new ResponseTypeDTO<List<AdminOrderResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get all orders successfully",
                Content = result
            };
        }

        public async Task<ResponseTypeDTO<AdminOrderResponseDTO>>
            GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.PaymentTransactions)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            return new ResponseTypeDTO<AdminOrderResponseDTO>
            {
                StatusCode = 200,
                Message = "Get order successfully",
                Content = BuildAdminOrderResponse(order)
            };
        }

        public async Task<ResponseTypeDTO<AdminOrderResponseDTO>>
            CompleteOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.PaymentTransactions)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            if (order.Status == COrderStatus.Completed)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Order is already completed",
                    Content = null
                };
            }

            if (order.Status == COrderStatus.Cancelled)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Cancelled order cannot be completed",
                    Content = null
                };
            }

            if (
                order.Status != COrderStatus.Paid ||
                order.PaymentStatus != CPaymentStatus.Succeeded
            )
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Only paid orders can be completed",
                    Content = null
                };
            }

            order.Status = COrderStatus.Completed;

            await _context.SaveChangesAsync();

            return new ResponseTypeDTO<AdminOrderResponseDTO>
            {
                StatusCode = 200,
                Message = "Order completed successfully",
                Content = BuildAdminOrderResponse(order)
            };
        }

        public async Task<ResponseTypeDTO<AdminOrderResponseDTO>>
            CancelOrderAsync(int orderId, CancelOrderDTO dto)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.PaymentTransactions)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            if (order.Status == COrderStatus.Cancelled)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Order is already cancelled",
                    Content = null
                };
            }

            if (order.Status == COrderStatus.Completed)
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Completed order cannot be cancelled",
                    Content = null
                };
            }

            if (
                order.Status == COrderStatus.Paid ||
                order.PaymentStatus == CPaymentStatus.Succeeded
            )
            {
                return new ResponseTypeDTO<AdminOrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Paid order cannot be cancelled because refund is not supported",
                    Content = null
                };
            }

            order.Status = COrderStatus.Cancelled;
            order.PaymentStatus = CPaymentStatus.Cancelled;
            order.CancelledAt = DateTime.Now;
            order.CancelReason = string.IsNullOrWhiteSpace(dto.CancelReason)
                ? "Cancelled by admin"
                : dto.CancelReason.Trim();

            await _context.SaveChangesAsync();

            return new ResponseTypeDTO<AdminOrderResponseDTO>
            {
                StatusCode = 200,
                Message = "Order cancelled successfully",
                Content = BuildAdminOrderResponse(order)
            };
        }

        private static AdminOrderResponseDTO BuildAdminOrderResponse(
            Order order
        )
        {
            return new AdminOrderResponseDTO
            {
                OrderId = order.OrderId,

                User = order.User == null
                    ? null
                    : new AdminOrderUserResponseDTO
                    {
                        UserId = order.User.UserId,
                        Username = order.User.Username,
                        FullName = order.User.FullName,
                        Email = order.User.Email,
                        Phone = order.User.Phone
                    },

                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                OrderDate = order.OrderDate,
                Note = order.Note,
                CancelledAt = order.CancelledAt,
                CancelReason = order.CancelReason,

                Items = order.OrderItems.Select(item =>
                    new OrderItemResponseDTO
                    {
                        OrderItemId = item.OrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.PriceAtTime,
                        SubTotal = item.PriceAtTime * item.Quantity
                    }
                ).ToList(),

                Payments = order.PaymentTransactions
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentTransactionResponseDTO
                    {
                        PaymentTransactionId = p.PaymentTransactionId,
                        OrderId = p.OrderId,
                        PaymentMethod = p.PaymentMethod,
                        Amount = p.Amount,
                        TransactionStatus = p.TransactionStatus,
                        VnpTxnRef = p.VnpTxnRef,
                        VnpTransactionNo = p.VnpTransactionNo,
                        VnpResponseCode = p.VnpResponseCode,
                        VnpTransactionStatus = p.VnpTransactionStatus,
                        VnpBankCode = p.VnpBankCode,
                        VnpPayDate = p.VnpPayDate,
                        CreatedAt = p.CreatedAt,
                        PaidAt = p.PaidAt
                    })
                    .ToList()
            };
        }
    }
}