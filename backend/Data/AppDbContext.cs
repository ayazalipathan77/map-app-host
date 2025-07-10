using Microsoft.EntityFrameworkCore;
using MapApp.Models;
using System;
using System.Collections.Generic;

namespace MapApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Pin> Pins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Pin entity if needed (e.g., primary key, required fields)
            modelBuilder.Entity<Pin>().HasKey(p => p.Id);

            // Seed initial data
            modelBuilder.Entity<Pin>().HasData(
                new Pin
                {
                    Id = Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef"),
                    Lat = 24.8607,
                    Lng = 67.0011,
                    Description = "Karachi Port - A major seaport in Pakistan.",
                    ImageUrls = new List<string> { "/images/default_port.jpg" } // Placeholder image
                },
                new Pin
                {
                    Id = Guid.Parse("b2c3d4e5-f6a7-8901-2345-67890abcdef0"),
                    Lat = 24.8567,
                    Lng = 67.0297,
                    Description = "Frere Hall - Historic building in Karachi.",
                    ImageUrls = new List<string> { "/images/default_frerehall.jpg" } // Placeholder image
                },
                new Pin
                {
                    Id = Guid.Parse("c3d4e5f6-a7b8-9012-3456-7890abcdef12"),
                    Lat = 24.7796,
                    Lng = 67.0278,
                    Description = "Clifton Beach - Popular recreational spot.",
                    ImageUrls = new List<string> { "/images/default_cliftonbeach.jpg" } // Placeholder image
                }
            );
        }
    }
}