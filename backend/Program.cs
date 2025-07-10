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

// Configure DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))); // Use connection string from appsettings.json

// Removed: builder.Services.AddSingleton<PinRepository>(sp =>
// {
//     var dataPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "pins.json");
//     return new PinRepository(dataPath);
// });

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
        ValidateIssuer = false, // Corrected: Added comma
        ValidateAudience = false, // Corrected: Added comma
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
    dbContext.Database.Migrate();
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