using System.Collections.Generic;
using System.IO; // Still needed for image handling, but not for pin data
using System.Linq;
using System.Text.Json; // No longer needed for pin data
using MapApp.Models;
using System;
using Microsoft.EntityFrameworkCore; // Added for DbContext and async methods
using System.Threading.Tasks; // Added for async methods

namespace MapApp.Data
{
    public class PinRepository
    {
        private readonly AppDbContext _context; // Changed to DbContext

        public PinRepository(AppDbContext context) // Changed constructor
        {
            _context = context;
        }

        // Removed LoadPins() and SavePins()

        public async Task AddPin(Pin pin) // Changed to async Task
        {
            if (pin.Id == Guid.Empty)
            {
                pin.Id = Guid.NewGuid();
            }
            _context.Pins.Add(pin);
            await _context.SaveChangesAsync(); // Use SaveChangesAsync
        }

        public async Task<Pin> GetPinById(Guid id) // Changed to async Task<Pin>
        {
            return await _context.Pins.FirstOrDefaultAsync(p => p.Id == id); // Use FirstOrDefaultAsync
        }

        public async Task UpdatePin(Pin updatedPin) // Changed to async Task
        {
            _context.Pins.Update(updatedPin); // EF Core tracks changes, so just update
            await _context.SaveChangesAsync(); // Use SaveChangesAsync
        }

        public async Task DeletePin(Guid id) // Changed to async Task
        {
            var pinToDelete = await _context.Pins.FirstOrDefaultAsync(p => p.Id == id); // Find pin first
            if (pinToDelete != null)
            {
                _context.Pins.Remove(pinToDelete);
                await _context.SaveChangesAsync(); // Use SaveChangesAsync
            }
        }

        public async Task<IEnumerable<Pin>> GetAllPins() // Changed to async Task<IEnumerable<Pin>>
        {
            return await _context.Pins.ToListAsync(); // Use ToListAsync
        }
    }
}