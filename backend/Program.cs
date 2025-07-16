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
Console.WriteLine("=== Configuring Cloudinary ===");
var cloudinaryUrl = builder.Configuration["CLOUDINARY_URL"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_URL");

if (string.IsNullOrEmpty(cloudinaryUrl))
{
    Console.WriteLine("❌ ERROR: CLOUDINARY_URL not found in configuration or environment variables");
    throw new InvalidOperationException("CLOUDINARY_URL not configured. Please set 'CLOUDINARY_URL' in appsettings.json or environment variables.");
}

try
{
    Console.WriteLine("🔧 Parsing Cloudinary URL...");
    Console.WriteLine($"   Raw URL format: {cloudinaryUrl.Substring(0, Math.Min(cloudinaryUrl.Length, 20))}...");

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
            Console.WriteLine($"   API Key: {(userInfo?.Length > 0 && !string.IsNullOrEmpty(userInfo[0]) ? "✅ Present" : "❌ Missing")}");
            Console.WriteLine($"   API Secret: {(userInfo?.Length > 1 && !string.IsNullOrEmpty(userInfo[1]) ? "✅ Present" : "❌ Missing")}");

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
            Console.WriteLine("✅ Using manual parsing approach");
        }
        catch (Exception parseEx)
        {
            Console.WriteLine($"❌ ERROR: Failed to parse Cloudinary URL manually: {parseEx.Message}");
            Console.WriteLine("🔄 Trying CloudinaryDotNet library parsing...");

            // Fallback to library parsing
            cloudinaryAccount = new CloudinaryDotNet.Account(cloudinaryUrl);
        }
    }
    else
    {
        Console.WriteLine("🔄 Using CloudinaryDotNet library parsing...");
        cloudinaryAccount = new CloudinaryDotNet.Account(cloudinaryUrl);
    }

    // Log account details (without sensitive info)
    Console.WriteLine($"✅ Cloudinary Account configured:");
    Console.WriteLine($"   Cloud Name: {cloudinaryAccount.Cloud}");
    Console.WriteLine($"   API Key: {(string.IsNullOrEmpty(cloudinaryAccount.ApiKey) ? "❌ Missing" : "✅ Present")}");
    Console.WriteLine($"   API Secret: {(string.IsNullOrEmpty(cloudinaryAccount.ApiSecret) ? "❌ Missing" : "✅ Present")}");

    // Validate required fields
    if (string.IsNullOrEmpty(cloudinaryAccount.Cloud))
    {
        Console.WriteLine("❌ ERROR: Cloud name is missing from Cloudinary configuration");
        throw new InvalidOperationException("Cloudinary Cloud name is required");
    }

    if (string.IsNullOrEmpty(cloudinaryAccount.ApiKey))
    {
        Console.WriteLine("❌ ERROR: API Key is missing from Cloudinary configuration");
        throw new InvalidOperationException("Cloudinary API Key is required");
    }

    if (string.IsNullOrEmpty(cloudinaryAccount.ApiSecret))
    {
        Console.WriteLine("❌ ERROR: API Secret is missing from Cloudinary configuration");
        throw new InvalidOperationException("Cloudinary API Secret is required");
    }

    var cloudinary = new Cloudinary(cloudinaryAccount);
    builder.Services.AddSingleton(cloudinary);

    Console.WriteLine("✅ Cloudinary service registered successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR: Failed to configure Cloudinary: {ex.Message}");
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
Console.WriteLine("=== Configuring JWT Authentication ===");
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrEmpty(jwtSecret))
{
    Console.WriteLine("❌ ERROR: JWT Secret not found in configuration or environment variables");
    throw new InvalidOperationException("JWT Secret not configured. Please set 'Jwt:Secret' in appsettings.json or environment variables.");
}

Console.WriteLine($"✅ JWT Secret configured (length: {jwtSecret.Length} characters)");
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
Console.WriteLine("=== Testing Cloudinary Connection ===");
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
            Console.WriteLine($"   API Response: {listResult.StatusCode}");
            Console.WriteLine($"   Resources found: {listResult.Resources?.Length ?? 0}");
            Console.WriteLine($"   Next cursor: {(string.IsNullOrEmpty(listResult.NextCursor) ? "None" : "Available")}");
        }
        else
        {
            Console.WriteLine($"⚠️  Cloudinary connection test returned: {listResult.StatusCode}");
            Console.WriteLine($"   Error: {listResult.Error?.Message ?? "Unknown error"}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: Cloudinary connection test failed: {ex.Message}");
        // Don't throw here - let the app start but log the issue
        Console.WriteLine("⚠️  Application will continue, but Cloudinary features may not work properly");
    }
}

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

Console.WriteLine("🚀 Application started successfully!");
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