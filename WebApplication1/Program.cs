using Braintree;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("NexelContextConnection")
    ?? throw new InvalidOperationException("Connection string 'NexelContextConnection' not found.");

builder.Services.AddDbContext<NexelContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<NexelContext>();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSingleton<BraintreeGateway>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new BraintreeGateway(
        configuration["Braintree:Environment"],
        configuration["Braintree:MerchantId"],
        configuration["Braintree:PublicKey"],
        configuration["Braintree:PrivateKey"]
    );
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    await DbSeeder.SeedRolesAndAdminAsync(serviceProvider); 
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();