using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Payments;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class AdminPaymentService : IAdminPaymentService
    {
        private readonly CreationStoreDbContext _context;

        public AdminPaymentService(CreationStoreDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseTypeDTO<List<AdminPaymentResponseDTO>>>
            GetAllPaymentsAsync()
        {
            var payments = await _context.PaymentTransactions
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = payments
                .Select(BuildAdminPaymentResponse)
                .ToList();

            return new ResponseTypeDTO<List<AdminPaymentResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get all payments successfully",
                Content = result
            };
        }

        public async Task<ResponseTypeDTO<AdminPaymentResponseDTO>>
            GetPaymentByIdAsync(int paymentTransactionId)
        {
            var payment = await _context.PaymentTransactions
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o.User)
                .FirstOrDefaultAsync(
                    p => p.PaymentTransactionId == paymentTransactionId
                );

            if (payment == null)
            {
                return new ResponseTypeDTO<AdminPaymentResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Payment transaction not found",
                    Content = null
                };
            }

            return new ResponseTypeDTO<AdminPaymentResponseDTO>
            {
                StatusCode = 200,
                Message = "Get payment successfully",
                Content = BuildAdminPaymentResponse(payment)
            };
        }

        public async Task<ResponseTypeDTO<List<AdminPaymentResponseDTO>>>
            GetPaymentsByOrderIdAsync(int orderId)
        {
            var orderExists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderId == orderId);

            if (!orderExists)
            {
                return new ResponseTypeDTO<List<AdminPaymentResponseDTO>>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            var payments = await _context.PaymentTransactions
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o.User)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = payments
                .Select(BuildAdminPaymentResponse)
                .ToList();

            return new ResponseTypeDTO<List<AdminPaymentResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get payments by order successfully",
                Content = result
            };
        }

        private static AdminPaymentResponseDTO BuildAdminPaymentResponse(
            PaymentTransaction payment
        )
        {
            return new AdminPaymentResponseDTO
            {
                PaymentTransactionId = payment.PaymentTransactionId,
                OrderId = payment.OrderId,

                Order = payment.Order == null
                    ? null
                    : new AdminPaymentOrderResponseDTO
                    {
                        OrderId = payment.Order.OrderId,
                        TotalAmount = payment.Order.TotalAmount,
                        Status = payment.Order.Status,
                        PaymentStatus = payment.Order.PaymentStatus,
                        OrderDate = payment.Order.OrderDate
                    },

                User = payment.Order?.User == null
                    ? null
                    : new AdminPaymentUserResponseDTO
                    {
                        UserId = payment.Order.User.UserId,
                        Username = payment.Order.User.Username,
                        FullName = payment.Order.User.FullName,
                        Email = payment.Order.User.Email,
                        Phone = payment.Order.User.Phone
                    },

                PaymentMethod = payment.PaymentMethod,
                Amount = payment.Amount,
                TransactionStatus = payment.TransactionStatus,
                VnpTxnRef = payment.VnpTxnRef,
                VnpTransactionNo = payment.VnpTransactionNo,
                VnpResponseCode = payment.VnpResponseCode,
                VnpTransactionStatus = payment.VnpTransactionStatus,
                VnpBankCode = payment.VnpBankCode,
                VnpPayDate = payment.VnpPayDate,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt
            };
        }
    }
}