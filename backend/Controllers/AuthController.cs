using Microsoft.AspNetCore.Mvc;
using MapApp.Models;
using System.IdentityModel.Tokens.Jwt; // Added
using System.Security.Claims; // Added
using System.Text; // Added
using Microsoft.IdentityModel.Tokens; // Added
using Microsoft.Extensions.Configuration; // Added for configuration access

namespace MapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration; // Added

        public AuthController(IConfiguration configuration) // Added constructor injection
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Hardcoded credentials for single-user admin login
            // In a real application, you would fetch user from DB and verify password hash
            if (request.Username == "ayaz" && request.Password == "ayaz12344321")
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSecret = _configuration["Jwt:Secret"];
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new InvalidOperationException("JWT Secret not configured.");
                }
                var key = Encoding.ASCII.GetBytes(jwtSecret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, request.Username),
                        // Add roles if applicable: new Claim(ClaimTypes.Role, "Admin")
                    }),
                    Expires = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new { token = tokenString, message = "Login successful" });
            }
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }
}