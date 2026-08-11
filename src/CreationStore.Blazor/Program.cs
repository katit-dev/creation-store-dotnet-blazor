using CreationStore.Blazor.Components;
using CreationStore.Blazor.Helpers;
using CreationStore.Blazor.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient gọi backend API
builder.Services.AddHttpClient("CreationStoreApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5041/");
});

// Cho phép inject HttpClient trực tiếp trong component/service
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>()
        .CreateClient("CreationStoreApi")
);

// DI TokenStorage
// DI State
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<UserStateService>();
builder.Services.AddScoped<ProductStateService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();