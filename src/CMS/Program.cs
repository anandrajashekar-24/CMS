using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services (controllers + Razor views).
builder.Services.AddControllersWithViews();

// Trust the X-Forwarded-For / X-Forwarded-Proto headers set by the Nginx
// reverse proxy in front of Kestrel, so the app sees the real client scheme/IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Nginx runs on the same host in this setup, so localhost is a trusted proxy.
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

// NOTE: When the REST API backend is added later, register an HttpClient here
// pointed at the API's base address, e.g.:
//
// builder.Services.AddHttpClient("Api", client =>
// {
//     client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. Adjust for production scenarios: https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
