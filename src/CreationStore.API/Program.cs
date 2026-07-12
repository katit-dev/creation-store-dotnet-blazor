using CreationStore.API.Data;
using CreationStore.API.Services.Implementations;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// DI Controllers
// ==========================
builder.Services.AddControllers();

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

app.UseAuthorization();

app.MapControllers();

app.Run();