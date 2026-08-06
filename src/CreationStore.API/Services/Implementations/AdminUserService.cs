using System.Security.Claims;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Users;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class AdminUserService : IAdminUserService
    {
        private readonly CreationStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminUserService(
            CreationStoreDbContext context,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseTypeDTO<List<AdminUserResponseDTO>>>
            GetAllUsersAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.UserId)
                .ToListAsync();

            var result = users
                .Select(BuildUserResponse)
                .ToList();

            return new ResponseTypeDTO<List<AdminUserResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get all users successfully",
                Content = result
            };
        }

        public async Task<ResponseTypeDTO<AdminUserDetailResponseDTO>>
            GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new ResponseTypeDTO<AdminUserDetailResponseDTO>
                {
                    StatusCode = 404,
                    Message = "User not found",
                    Content = null
                };
            }

            var orderCount = await _context.Orders
                .AsNoTracking()
                .CountAsync(o => o.UserId == userId);

            var totalSpent = await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.UserId == userId &&
                    o.PaymentStatus == CPaymentStatus.Succeeded
                )
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var response = new AdminUserDetailResponseDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleIds = user.UserRoles
                    .Select(ur => ur.RoleId)
                    .ToList(),
                Roles = user.UserRoles
                    .Select(ur => ur.Role.RoleName)
                    .ToList(),
                OrderCount = orderCount,
                TotalSpent = totalSpent
            };

            return new ResponseTypeDTO<AdminUserDetailResponseDTO>
            {
                StatusCode = 200,
                Message = "Get user successfully",
                Content = response
            };
        }

        public async Task<ResponseTypeDTO<AdminUserResponseDTO>>
            ChangeUserRoleAsync(int userId, AdminChangeUserRoleDTO dto)
        {
            // kiem tra roleId nhap co hop le khong
            if (dto.RoleId <= 0)
            {
                return new ResponseTypeDTO<AdminUserResponseDTO>
                {
                    StatusCode = 400,
                    Message = "RoleId is required",
                    Content = null
                };
            }

            // lay admin hien tai tu token
            var currentAdminUserId = GetCurrentUserId();
            // khong cho admin tu doi role cua chinh minh
            if (currentAdminUserId == userId)
            {
                return new ResponseTypeDTO<AdminUserResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Admin cannot change own role",
                    Content = null
                };
            }

            // kiem tra roleId trong dto co hop le khong
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == dto.RoleId);

            if (role == null)
            {
                return new ResponseTypeDTO<AdminUserResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Invalid role",
                    Content = null
                };
            }

            // tim user theo userId duoc truyen vao
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new ResponseTypeDTO<AdminUserResponseDTO>
                {
                    StatusCode = 404,
                    Message = "User not found",
                    Content = null
                };
            }

            // neu tim thay user
            // tiep tuc kiem tra user do co dang giu role admin khong
            var isTargetAdmin = user.UserRoles
                .Any(ur => ur.RoleId == CRole.Admin);

            // neu dang la admin && va muon doi sang role # admin
            var isChangingAdminToOtherRole =
                isTargetAdmin && dto.RoleId != CRole.Admin;

            // neu doi tu admin -> other
            if (isChangingAdminToOtherRole)
            {
                var adminCount = await _context.UserRoles
                    .Where(ur => ur.RoleId == CRole.Admin)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();
                // chan khong cho phep chuyen role cua admin cuoi cung trong he thong
                // vi neu chi con 1 admin, ma doi thi khong con admin nao trong he thong
                if (adminCount <= 1)
                {
                    return new ResponseTypeDTO<AdminUserResponseDTO>
                    {
                        StatusCode = 400,
                        Message = "Cannot change role of the last admin",
                        Content = null
                    };
                }
            }
            
            // Neu khong roi vao truong hop tren
            // thi thuc hien xoa role cu va them role moi
            var oldRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            _context.UserRoles.RemoveRange(oldRoles);

            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = dto.RoleId
            });

            await _context.SaveChangesAsync();

            // sau khi doi thi lay user do ra de hien thi
            var updatedUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.UserId == userId);

            return new ResponseTypeDTO<AdminUserResponseDTO>
            {
                StatusCode = 200,
                Message = "User role updated successfully",
                Content = BuildUserResponse(updatedUser)
            };
        }

        private int GetCurrentUserId()
        {
            var userIdValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                return 0;
            }

            return userId;
        }

        private static AdminUserResponseDTO BuildUserResponse(User user)
        {
            return new AdminUserResponseDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleIds = user.UserRoles
                    .Select(ur => ur.RoleId)
                    .ToList(),
                Roles = user.UserRoles
                    .Select(ur => ur.Role.RoleName)
                    .ToList()
            };
        }
    }
}