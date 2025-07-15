using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace MapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        [HttpGet("search")]
        public IActionResult SearchLocations([FromQuery] string query)
        {
            // In a real application, you would integrate with a geocoding service here
            // For demonstration, we'll return a hardcoded list of Karachi landmarks
            var karachiLandmarks = new List<object>
            {
                new { Name = "Clifton Beach", Lat = 24.7796, Lng = 67.0278 },
                new { Name = "Frere Hall", Lat = 24.8567, Lng = 67.0297 },
                new { Name = "Mohatta Palace", Lat = 24.8193, Lng = 67.0363 },
                new { Name = "Quaid's Mausoleum", Lat = 24.8636, Lng = 67.0311 }
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(karachiLandmarks); // Return all if query is empty
            }

            var results = karachiLandmarks
                .Where(l => 
                {
                    var name = l.GetType().GetProperty("Name")?.GetValue(l)?.ToString();
                    return name != null && name.ToLower().Contains(query.ToLower());
                })
                .ToList();

            return Ok(results);
        }
    }
}