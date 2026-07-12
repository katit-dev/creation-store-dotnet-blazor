using System.Security.Claims;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly CreationStoreDbContext _context;
        private readonly JwtAuthService _jwtAuthService;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AuthService(
            CreationStoreDbContext context,
            JwtAuthService jwtAuthService,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _context = context;
            _jwtAuthService = jwtAuthService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResponseTypeDTO<RegisterResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return new ResponseTypeDTO<RegisterResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Username is required",
                    Content = null
                };
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return new ResponseTypeDTO<RegisterResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Password is required",
                    Content = null
                };
            }

            if (dto.Password.Length < 6)
            {
                return new ResponseTypeDTO<RegisterResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Password must be at least 6 characters",
                    Content = null
                };
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return new ResponseTypeDTO<RegisterResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Full name is required",
                    Content = null
                };
            }

            var username = dto.Username.Trim();
            var fullName = dto.FullName.Trim();
            var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == username);

            if (usernameExists)
            {
                return new ResponseTypeDTO<RegisterResponseDTO>
                {
                    StatusCode = 409,
                    Message = "Username already exists",
                    Content = null
                };
            }

            if (email != null)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == email);

                if (emailExists)
                {
                    return new ResponseTypeDTO<RegisterResponseDTO>
                    {
                        StatusCode = 409,
                        Message = "Email already exists",
                        Content = null
                    };
                }
            }

            if (phone != null)
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.Phone == phone);

                if (phoneExists)
                {
                    return new ResponseTypeDTO<RegisterResponseDTO>
                    {
                        StatusCode = 409,
                        Message = "Phone already exists",
                        Content = null
                    };
                }
            }

            var user = new User
            {
                Username = username,
                PasswordHash = HelperFunction.HashPassword(dto.Password),
                FullName = fullName,
                Email = email,
                Phone = phone,
                IsActive = true,
                CreatedAt = System.DateTime.Now
            };

            var userRole = new UserRole
            {
                RoleId = CRole.Member,
                Description = "Set role Member when user registers"
            };

            user.UserRoles.Add(userRole);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return new ResponseTypeDTO<RegisterResponseDTO>
            {
                StatusCode = 201,
                Message = "Register successfully. Please login to continue.",
                Content = new RegisterResponseDTO
                {
                    UserId = user.UserId,
                    Username = user.Username
                }
            };
        }

        public async Task<ResponseTypeDTO<LoginResponseDTO>> LoginAsync(LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.LoginIdentifier))
            {
                return new ResponseTypeDTO<LoginResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Username, email or phone is required",
                    Content = null
                };
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                return new ResponseTypeDTO<LoginResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Password is required",
                    Content = null
                };
            }

            var loginIdentifier = dto.LoginIdentifier.Trim();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.IsActive &&
                    (
                        u.Username == loginIdentifier ||
                        u.Email == loginIdentifier ||
                        u.Phone == loginIdentifier
                    )
                );

            if (user == null)
            {
                return new ResponseTypeDTO<LoginResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid username",
                    Content = null
                };
            }

            var isPasswordValid = HelperFunction.VerifyPassword(
                dto.Password,
                user.PasswordHash
            );

            if (!isPasswordValid)
            {
                return new ResponseTypeDTO<LoginResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid password",
                    Content = null
                };
            }

            var roles = user.UserRoles
                .Select(ur => ur.Role.RoleName)
                .ToList();

            var token = _jwtAuthService.GenerateToken(
                user,
                roles,
                out DateTime expiresAt
            );

            return new ResponseTypeDTO<LoginResponseDTO>
            {
                StatusCode = 200,
                Message = "Login successfully",
                Content = new LoginResponseDTO
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = expiresAt
                }
            };
        }

        public async Task<ResponseTypeDTO<UserProfileDTO>> GetMeAsync()
        {
            var userIdValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!int.TryParse(userIdValue, out int userId))
            {
                return new ResponseTypeDTO<UserProfileDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                return new ResponseTypeDTO<UserProfileDTO>
                {
                    StatusCode = 404,
                    Message = "User not found",
                    Content = null
                };
            }

            return new ResponseTypeDTO<UserProfileDTO>
            {
                StatusCode = 200,
                Message = "Get profile successfully",
                Content = new UserProfileDTO
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Roles = user.UserRoles
                        .Select(ur => ur.Role.RoleName)
                        .ToList()
                }
            };
        }
    }
}