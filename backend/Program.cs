using MapApp.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.EntityFrameworkCore; // Added
using Microsoft.AspNetCore.Authentication.JwtBearer; // Added
using Microsoft.IdentityModel.Tokens; // Added
using System.Text; // Added
using System; // Added for Guid

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = GetConnectionString(builder);
    options.UseNpgsql(connectionString);
});

// Register PinRepository to use AppDbContext
builder.Services.AddScoped<PinRepository>(); // Changed to Scoped as DbContext is Scoped

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT Secret not configured. Please set 'Jwt:Secret' in appsettings.json or environment variables.");
}
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero // No leeway for token expiration
    };
});

builder.Services.AddAuthorization(); // Added

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        Console.WriteLine("Applying database migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "" // Serve from root
});

app.UseRouting();

app.UseCors(); // Use CORS

app.UseAuthentication(); // Added: Must be before UseAuthorization
app.UseAuthorization(); // Existing

app.MapControllers();

app.Run();

// Helper method to get connection string
static string GetConnectionString(WebApplicationBuilder builder)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    Console.WriteLine($"DATABASE_URL exists: {!string.IsNullOrEmpty(databaseUrl)}");

    if (!string.IsNullOrEmpty(databaseUrl))
    {
        try
        {
            // Parse Render's DATABASE_URL format: postgresql://username:password@host:port/database
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var port = uri.Port == -1 ? 5432 : uri.Port;

            if (userInfo.Length != 2)
            {
                throw new InvalidOperationException("Invalid DATABASE_URL format - user info should contain username:password");
            }

            var connectionString = $"Host={uri.Host};Port={port};Database={uri.LocalPath.Substring(1)};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

            Console.WriteLine($"Using DATABASE_URL connection string for host: {uri.Host}");
            return connectionString;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse DATABASE_URL: {ex.Message}");
        }
    }

    // Fallback to local connection string
    var fallbackConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(fallbackConnectionString))
    {
        throw new InvalidOperationException("Neither DATABASE_URL environment variable nor 'DefaultConnection' in appsettings.json is set.");
    }

    Console.WriteLine("Using fallback connection string from appsettings.json");
    return fallbackConnectionString;
}