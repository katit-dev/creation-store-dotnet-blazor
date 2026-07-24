using System.Security.Claims;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly CreationStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(
            CreationStoreDbContext context,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ============================================================
        // CHECKOUT
        // Mục đích:
        // - Tạo order từ cart Active hiện tại của user
        // - UserId lấy từ token, không lấy từ client
        // - Copy CartItems sang OrderItems
        // - Đổi cart status thành CheckedOut
        // - Order sau khi tạo có Status = PendingPayment
        // ============================================================
        public async Task<ResponseTypeDTO<OrderResponseDTO>> CheckoutAsync(
            CheckoutOrderDTO dto
        )
        {
            // lay userId tu token
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            // lay cart hien tai cua user
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId.Value &&
                    c.Status == CCartStatus.Active
                );

            if (cart == null || !cart.CartItems.Any())
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Cart is empty",
                    Content = null
                };
            }

            // transaction
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var totalAmount = cart.CartItems
                    .Sum(ci => ci.PriceAtTime * ci.Quantity);
                // tao order
                var order = new Order
                {
                    UserId = userId.Value,
                    TotalAmount = totalAmount,
                    Status = COrderStatus.PendingPayment,
                    PaymentStatus = CPaymentStatus.Pending,
                    OrderDate = DateTime.Now,
                    Note = dto.Note,
                    CancelledAt = null,
                    CancelReason = null
                };

                // them cac item vao order
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Product.ProductName,
                        Quantity = cartItem.Quantity,
                        PriceAtTime = cartItem.PriceAtTime
                    };

                    order.OrderItems.Add(orderItem);
                }

                _context.Orders.Add(order);

                cart.Status = CCartStatus.CheckedOut;
                cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var orderResponse =
                    await BuildOrderResponseAsync(order.OrderId);

                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 201,
                    Message = "Checkout successfully",
                    Content = orderResponse
                };
            }
            catch
            {
                await transaction.RollbackAsync();

                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Checkout failed",
                    Content = null
                };
            }
        }

        // ============================================================
        // GET MY ORDERS
        // Mục đích:
        // - Lấy danh sách order của user đang đăng nhập
        // - User chỉ xem được order của chính mình
        // ============================================================
        public async Task<ResponseTypeDTO<List<OrderResponseDTO>>> GetMyOrdersAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<List<OrderResponseDTO>>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = orders
                .Select(MapOrderToResponse)
                .ToList();

            return new ResponseTypeDTO<List<OrderResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get orders successfully",
                Content = result
            };
        }

        // ============================================================
        // GET MY ORDER BY ID
        // Mục đích:
        // - Xem chi tiết một order
        // - Chỉ cho user xem order thuộc về chính họ
        // ============================================================
        public async Task<ResponseTypeDTO<OrderResponseDTO>> GetMyOrderByIdAsync(
            int orderId
        )
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId.Value
                );

            if (order == null)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            return new ResponseTypeDTO<OrderResponseDTO>
            {
                StatusCode = 200,
                Message = "Get order successfully",
                Content = MapOrderToResponse(order)
            };
        }

        // ============================================================
        // CANCEL ORDER
        // Mục đích:
        // - User hủy order của chính họ
        // - Chỉ cho hủy order đang PendingPayment
        // - Nếu order đã Paid/Completed thì không cho hủy
        // ============================================================
        public async Task<ResponseTypeDTO<OrderResponseDTO>> CancelOrderAsync(
            int orderId,
            CancelOrderDTO dto
        )
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId.Value
                );

            if (order == null)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            if (order.Status == COrderStatus.Cancelled)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Order already cancelled",
                    Content = null
                };
            }

            if (order.Status != COrderStatus.PendingPayment)
            {
                return new ResponseTypeDTO<OrderResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Only pending payment orders can be cancelled",
                    Content = null
                };
            }

            order.Status = COrderStatus.Cancelled;
            order.PaymentStatus = CPaymentStatus.Cancelled;
            order.CancelledAt = DateTime.Now;
            order.CancelReason = dto.CancelReason;

            await _context.SaveChangesAsync();

            var orderResponse =
                await BuildOrderResponseAsync(order.OrderId);

            return new ResponseTypeDTO<OrderResponseDTO>
            {
                StatusCode = 200,
                Message = "Cancel order successfully",
                Content = orderResponse
            };
        }

        // ============================================================
        // GET CURRENT USER ID FROM TOKEN
        // Mục đích:
        // - Lấy UserId từ JWT token
        // - Không lấy UserId từ client
        // ============================================================
        private int? GetCurrentUserId()
        {
            var userIdValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!int.TryParse(userIdValue, out int userId))
            {
                return null;
            }

            return userId;
        }

        // ============================================================
        // BUILD ORDER RESPONSE
        // Mục đích:
        // - Load lại order từ DB
        // - Convert Order model sang OrderResponseDTO
        // ============================================================
        private async Task<OrderResponseDTO> BuildOrderResponseAsync(
            int orderId
        )
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstAsync(o => o.OrderId == orderId);

            return MapOrderToResponse(order);
        }

        // ============================================================
        // MAP ORDER TO RESPONSE DTO
        // Mục đích:
        // - Convert Order entity sang DTO
        // - Tính SubTotal cho từng item
        // ============================================================
        private static OrderResponseDTO MapOrderToResponse(Order order)
        {
            var items = order.OrderItems
                .Select(oi => new OrderItemResponseDTO
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.PriceAtTime,
                    SubTotal = oi.PriceAtTime * oi.Quantity
                })
                .ToList();

            return new OrderResponseDTO
            {
                OrderId = order.OrderId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                OrderDate = order.OrderDate,
                Note = order.Note,
                CancelledAt = order.CancelledAt,
                CancelReason = order.CancelReason,
                Items = items
            };
        }
    }
}