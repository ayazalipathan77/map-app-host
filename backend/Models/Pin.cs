
using System; // Add this for Guid
using System.Collections.Generic;

namespace MapApp.Models
{
    public class Pin
    {
        public Guid Id { get; set; } // Add this line
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? Description { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}
