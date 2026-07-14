using System.Text;
using CreationStore.API.Data;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers;
using CreationStore.API.Services.Implementations;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// DI Controllers
// ==========================
builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errorMessage = context.ModelState
            .Where(x => x.Value != null && x.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors)
            .Select(x => x.ErrorMessage)
            .FirstOrDefault();

        var response = new ResponseTypeDTO<object>
        {
            StatusCode = 400,
            Message = errorMessage ?? "Invalid request data",
            Content = null
        };

        return new BadRequestObjectResult(response);
    };
});

// ==========================
// DI DbContext
// ==========================
builder.Services.AddDbContext<CreationStoreDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("CreationStoreDb"));
});

// ==========================
// DI Services
// ==========================
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// JwtAuthService dùng để tạo JWT token
builder.Services.AddScoped<JwtAuthService>();

// DI HttpContextAccessor
// Để lấy thông tin userId từ JWT trong AuthService
builder.Services.AddHttpContextAccessor();

// ==========================
// DI Swagger
// ==========================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Creation Store API",
        Version = "v1",
        Description = "API documentation for Creation Store project"
    });

    // Khai báo Bearer token cho Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Ví dụ: Bearer eyJhbGciOi..."
    });

    // Áp dụng Bearer token cho các API có Authorize
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// ==========================
// DI CORS
// Cho phép Blazor gọi API
// Nếu 7188/5099 là port của Blazor thì giữ nguyên.
// Nếu đây là port API thì phải đổi lại theo port Blazor.
// ==========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7188",
                "http://localhost:5099"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ==========================
// DI Authentication - JWT
// ==========================
var jwtKey = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("JWT Key is missing");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Kiểm tra token có đúng nơi phát hành không
        ValidateIssuer = true,

        // Kiểm tra token có đúng client nhận không
        ValidateAudience = true,

        // Kiểm tra token còn hạn không
        ValidateLifetime = true,

        // Kiểm tra chữ ký token có đúng secret key không
        ValidateIssuerSigningKey = true,

        ValidIssuer = issuer,
        ValidAudience = audience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),

        // Không cộng thêm thời gian trễ khi token hết hạn
        ClockSkew = TimeSpan.Zero
    };
});


// ==========================
// DI Authorization
// Chưa làm JWT nên tạm để Authorization cơ bản
// ==========================
builder.Services.AddAuthorization();

var app = builder.Build();

// ==========================
// Swagger Middleware
// ==========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Creation Store API v1");
        options.RoutePrefix = "swagger";
    });
}

// ==========================
// Exception Handler Middleware
// ==========================
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var result = new
        {
            StatusCode = 500,
            Message = "Có lỗi xảy ra trong hệ thống.",
            Content = (object?)null,
            DateTime = System.DateTime.Now
        };

        await context.Response.WriteAsJsonAsync(result);
    });
});

app.UseHttpsRedirection();

// ==========================
// CORS Middleware
// Đặt trước Authorization
// ==========================
app.UseCors("AllowBlazorClient");

// Sau này làm JWT thì thêm ở đây:
// app.UseAuthentication();

app.UseAuthentication(); // Thêm middleware xác thực JWT trước Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
// dong này giúp test project gọi được API project bằng WebApplicationFactory<Program> trong xUnit test project