using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note_taking_demo.Data;
using Note_taking_demo.Models;

namespace Note_taking_demo.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<IActionResult> RegisterAsync(string email, string password)
    {
        // does the user already exist?
        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists)
        {
            return new BadRequestObjectResult("User with this email already exists.");
        }

        // Create the user object
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.HashPassword(password)
        };

        // Save to database
        if (user != null) {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();     
        
        } else {
            return new BadRequestObjectResult("Error creating user.");
        }

        // log in user after registration
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return new StatusCodeResult(500);
        }
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };
        
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
        
        return new OkResult();
    }
    

}
