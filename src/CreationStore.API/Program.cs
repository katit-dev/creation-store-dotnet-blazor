using CreationStore.API.Data;
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
// Nếu đây là port Blazor của bạn thì giữ nguyên.
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
// Chưa làm JWT nên tạm thời chỉ đăng ký Authorization cơ bản
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
            IsSuccess = false,
            Message = "Có lỗi xảy ra trong hệ thống."
        };

        await context.Response.WriteAsJsonAsync(result);
    });
});

app.UseHttpsRedirection();

// Dùng CORS trước Authorization
app.UseCors("AllowBlazorClient");

// Sau này làm JWT thì thêm app.UseAuthentication() ở đây
// app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();