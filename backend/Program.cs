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
using CloudinaryDotNet; // Added for Cloudinary

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Cloudinary with enhanced logging and validation

var cloudinaryUrl = builder.Configuration["CLOUDINARY_URL"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_URL");

if (string.IsNullOrEmpty(cloudinaryUrl))
{
    
    throw new InvalidOperationException("CLOUDINARY_URL not configured. Please set 'CLOUDINARY_URL' in appsettings.json or environment variables.");
}

try
{
    
    

    CloudinaryDotNet.Account cloudinaryAccount;

    // Try to parse the URL manually and create account with individual components
    if (cloudinaryUrl.StartsWith("cloudinary://"))
    {
        try
        {
            var uri = new Uri(cloudinaryUrl);
            var userInfo = uri.UserInfo?.Split(':');
            var cloudName = uri.Host;

            Console.WriteLine($"   Parsed Cloud Name: {cloudName}");
            

            if (string.IsNullOrEmpty(cloudName))
            {
                throw new InvalidOperationException("Cloud name is missing from Cloudinary URL");
            }

            if (userInfo?.Length < 2 || string.IsNullOrEmpty(userInfo[0]) || string.IsNullOrEmpty(userInfo[1]))
            {
                throw new InvalidOperationException("API Key and Secret are missing from Cloudinary URL");
            }

            // Create account with individual components (more reliable)
            cloudinaryAccount = new CloudinaryDotNet.Account(cloudName, userInfo[0], userInfo[1]);
            
        }
        catch (Exception parseEx)
        {
            
            

            // Fallback to library parsing
            cloudinaryAccount = new CloudinaryDotNet.Account(cloudinaryUrl);
        }
    }
    else
    {
        
        cloudinaryAccount = new CloudinaryDotNet.Account(cloudinaryUrl);
    }

    // Log account details (without sensitive info)
    
    

    // Validate required fields
    if (string.IsNullOrEmpty(cloudinaryAccount.Cloud))
    {
        
        throw new InvalidOperationException("Cloudinary Cloud name is required");
    }

    if (string.IsNullOrEmpty(cloudinaryAccount.ApiKey))
    {
        
        throw new InvalidOperationException("Cloudinary API Key is required");
    }

    if (string.IsNullOrEmpty(cloudinaryAccount.ApiSecret))
    {
        
        throw new InvalidOperationException("Cloudinary API Secret is required");
    }

    var cloudinary = new Cloudinary(cloudinaryAccount);
    builder.Services.AddSingleton(cloudinary);

    
}
catch (Exception ex)
{
    
    throw new InvalidOperationException($"Failed to configure Cloudinary: {ex.Message}", ex);
}

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = GetConnectionString(builder);
    options.UseNpgsql(connectionString);
});

// Register PinRepository to use AppDbContext
builder.Services.AddScoped<PinRepository>(); // Changed to Scoped as DbContext is Scoped

// Configure JWT Authentication with enhanced logging

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");

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

// Test Cloudinary connection on startup

using (var scope = app.Services.CreateScope())
{
    try
    {
        var cloudinary = scope.ServiceProvider.GetRequiredService<Cloudinary>();

        // Test connection by making a simple API call - list resources
        var listResult = cloudinary.ListResources();

        if (listResult.StatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine("✅ Cloudinary connection test successful!");
            
        }
        else
        {
            
            
        }
    }
    catch (Exception ex)
    {
        
        // Don't throw here - let the app start but log the issue
        
    }
}

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        
        dbContext.Database.Migrate();
        
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

app.UseDefaultFiles(); // Enables default document for the current path
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

    
    return fallbackConnectionString;
}