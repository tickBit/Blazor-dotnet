using Note_taking_demo.Components;
using Note_taking_demo.Data;
using Note_taking_demo.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Note_taking_demo.Models;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<InfoService>();
builder.Services.AddHttpContextAccessor();
    
// Configure DbContext with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (
    HttpContext httpContext,
    AppDbContext db) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var user = await db.Users
        .SingleOrDefaultAsync(u => u.Email == email);

    if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
    {
        return Results.Redirect("/login?error=invalid%20credentials");
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };

    var identity = new ClaimsIdentity(
        claims,
        CookieAuthenticationDefaults.AuthenticationScheme);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Redirect("/infos");
});



app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

    return Results.Redirect("/");
});


app.MapPost("/auth/register", async (
    HttpContext httpContext,
    AppDbContext db) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/register?error=Fill all fields");
    }

    if (password != confirmPassword)
    {
        return Results.Redirect("/register?error=Password mismatch");
    }
    
    if (await db.Users.AnyAsync(u => u.Email == email))
    {
        return Results.Redirect("/register?error=exists");
    }

    var user = new User
    {
        Email = email,
        PasswordHash = PasswordHasher.HashPassword(password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    // log in user
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };

    var identity = new ClaimsIdentity(
        claims,
        CookieAuthenticationDefaults.AuthenticationScheme);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    return Results.Redirect("/infos");
});

app.MapPost("/auth/delete-account", async (
    HttpContext httpContext,
    AppDbContext db) =>
{
    var user = httpContext.User;

    var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
    if (idClaim == null)
    {
        return Results.Redirect("/login");
    }

    int userId = int.Parse(idClaim.Value);

    var dbUser = await db.Users.FindAsync(userId);
    if (dbUser == null)
    {
        return Results.Redirect("/login");
    }

    // Delete user from database
    db.Users.Remove(dbUser);
    await db.SaveChangesAsync();


    // Log out
    await httpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

    return Results.Redirect("/");
});

app.Run();
