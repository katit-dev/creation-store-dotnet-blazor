using CreationStore.Blazor.Components;

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