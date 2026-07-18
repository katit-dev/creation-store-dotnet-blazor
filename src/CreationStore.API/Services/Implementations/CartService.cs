using System.Security.Claims;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Cart;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly CreationStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(
            CreationStoreDbContext context,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ============================================================
        // GET MY CART
        // Mục đích:
        // - Lấy giỏ hàng hiện tại của user đang đăng nhập
        // - UserId không lấy từ client
        // - UserId được lấy từ JWT token
        // - Nếu user chưa có cart Active thì tự tạo cart mới
        // ============================================================
        public async Task<ResponseTypeDTO<CartResponseDTO>> GetMyCartAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var cart = await GetOrCreateActiveCartAsync(userId.Value);

            var cartResponse = await BuildCartResponseAsync(cart.CartId);

            return new ResponseTypeDTO<CartResponseDTO>
            {
                StatusCode = 200,
                Message = "Get cart successfully",
                Content = cartResponse
            };
        }

        // ============================================================
        // ADD ITEM TO CART
        // Mục đích:
        // - Thêm sản phẩm vào giỏ hàng của user đang đăng nhập
        // - Nếu cart chưa tồn tại thì tự tạo cart
        // - Nếu sản phẩm đã có trong cart thì cộng thêm số lượng
        // - Nếu chưa có thì tạo CartItem mới
        // - Giá lưu vào CartItem là PriceAtTime
        // ============================================================
        public async Task<ResponseTypeDTO<CartResponseDTO>> AddItemAsync(
            AddCartItemDTO dto
        )
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProductId == dto.ProductId &&
                    p.IsActive
                );

            if (product == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Product not found",
                    Content = null
                };
            }

            var cart = await GetOrCreateActiveCartAsync(userId.Value);

            var existingItem = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    PriceAtTime = product.Price,
                    CreatedAt = System.DateTime.Now
                };

                cart.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();

            var cartResponse = await BuildCartResponseAsync(cart.CartId);

            return new ResponseTypeDTO<CartResponseDTO>
            {
                StatusCode = 200,
                Message = "Add item to cart successfully",
                Content = cartResponse
            };
        }

        // ============================================================
        // UPDATE CART ITEM QUANTITY
        // Mục đích:
        // - Cập nhật số lượng của một CartItem
        // - Chỉ được update item thuộc cart của user đang đăng nhập
        // - Không cho user sửa cart item của user khác
        // ============================================================
        public async Task<ResponseTypeDTO<CartResponseDTO>> UpdateItemAsync(
            int cartItemId,
            UpdateCartItemDTO dto
        )
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var cart = await GetActiveCartWithItemsAsync(userId.Value);

            if (cart == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Cart not found",
                    Content = null
                };
            }

            var cartItem = cart.CartItems
                .FirstOrDefault(ci => ci.CartItemId == cartItemId);

            if (cartItem == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Cart item not found",
                    Content = null
                };
            }

            cartItem.Quantity = dto.Quantity;
            cart.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();

            var cartResponse = await BuildCartResponseAsync(cart.CartId);

            return new ResponseTypeDTO<CartResponseDTO>
            {
                StatusCode = 200,
                Message = "Update cart item successfully",
                Content = cartResponse
            };
        }

        // ============================================================
        // REMOVE CART ITEM
        // Mục đích:
        // - Xóa một item khỏi cart
        // - Chỉ xóa item trong cart của user đang đăng nhập
        // - Không cho user xóa item của cart người khác
        // ============================================================
        public async Task<ResponseTypeDTO<CartResponseDTO>> RemoveItemAsync(
            int cartItemId
        )
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var cart = await GetActiveCartWithItemsAsync(userId.Value);

            if (cart == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Cart not found",
                    Content = null
                };
            }

            var cartItem = cart.CartItems
                .FirstOrDefault(ci => ci.CartItemId == cartItemId);

            if (cartItem == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Cart item not found",
                    Content = null
                };
            }

            _context.CartItems.Remove(cartItem);

            cart.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();

            var cartResponse = await BuildCartResponseAsync(cart.CartId);

            return new ResponseTypeDTO<CartResponseDTO>
            {
                StatusCode = 200,
                Message = "Remove cart item successfully",
                Content = cartResponse
            };
        }

        // ============================================================
        // CLEAR CART
        // Mục đích:
        // - Xóa toàn bộ item trong cart của user đang đăng nhập
        // - Cart vẫn còn, chỉ xóa các CartItems
        // ============================================================
        public async Task<ResponseTypeDTO<CartResponseDTO>> ClearCartAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var cart = await GetActiveCartWithItemsAsync(userId.Value);

            if (cart == null)
            {
                return new ResponseTypeDTO<CartResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Cart not found",
                    Content = null
                };
            }

            _context.CartItems.RemoveRange(cart.CartItems);

            cart.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();

            var cartResponse = await BuildCartResponseAsync(cart.CartId);

            return new ResponseTypeDTO<CartResponseDTO>
            {
                StatusCode = 200,
                Message = "Clear cart successfully",
                Content = cartResponse
            };
        }

        // ============================================================
        // GET CURRENT USER ID FROM TOKEN
        // Mục đích:
        // - Lấy UserId từ JWT token
        // - UserId nằm trong ClaimTypes.NameIdentifier
        // - Nếu token không hợp lệ thì return null
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
        // GET OR CREATE ACTIVE CART
        // Mục đích:
        // - Tìm cart Active của user
        // - Nếu có rồi thì trả cart đó
        // - Nếu chưa có thì tạo cart mới
        // - Dùng cho GetMyCart và AddItem
        // ============================================================
        private async Task<Cart> GetOrCreateActiveCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.Status == CCartStatus.Active
                );

            if (cart != null)
            {
                return cart;
            }

            cart = new Cart
            {
                UserId = userId,
                Status = CCartStatus.Active,
                CreatedAt = System.DateTime.Now,
                UpdatedAt = System.DateTime.Now
            };

            _context.Carts.Add(cart);

            await _context.SaveChangesAsync();

            return cart;
        }

        // ============================================================
        // GET ACTIVE CART WITH ITEMS
        // Mục đích:
        // - Lấy cart Active của user kèm danh sách CartItems
        // - Hàm này KHÔNG tự tạo cart mới
        // - Dùng cho Update, Remove, Clear
        // ============================================================
        private async Task<Cart?> GetActiveCartWithItemsAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.Status == CCartStatus.Active
                );
        }

        // ============================================================
        // BUILD CART RESPONSE DTO
        // Mục đích:
        // - Convert từ Cart model sang CartResponseDTO
        // - Không trả trực tiếp entity Cart ra ngoài API
        // - Tính TotalItems và TotalAmount
        // - Dùng PriceAtTime để giữ giá tại thời điểm thêm vào cart
        // ============================================================
        private async Task<CartResponseDTO> BuildCartResponseAsync(int cartId)
        {
            var cart = await _context.Carts
                .AsNoTracking()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstAsync(c => c.CartId == cartId);

            var items = cart.CartItems
                .Select(ci => new CartItemResponseDTO
                {
                    CartItemId = ci.CartItemId,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductName,
                    UnitPrice = ci.PriceAtTime,
                    Quantity = ci.Quantity,
                    SubTotal = ci.PriceAtTime * ci.Quantity
                })
                .ToList();

            return new CartResponseDTO
            {
                CartId = cart.CartId,
                Status = cart.Status,
                Items = items,
                TotalItems = items.Sum(i => i.Quantity),
                TotalAmount = items.Sum(i => i.SubTotal)
            };
        }
    }
}