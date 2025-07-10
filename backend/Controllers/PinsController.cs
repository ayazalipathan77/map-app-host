using Microsoft.AspNetCore.Mvc;
using MapApp.Models;
using MapApp.Data;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace MapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PinsController : ControllerBase
    {
        private readonly PinRepository _pinRepository;
        private readonly IWebHostEnvironment _env;

        public PinsController(PinRepository pinRepository, IWebHostEnvironment env)
        {
            _pinRepository = pinRepository;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetPins()
        {
            return Ok(await _pinRepository.GetAllPins());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPin(Guid id)
        {
            var pin = await _pinRepository.GetPinById(id);
            if (pin == null)
            {
                return NotFound();
            }
            return Ok(pin);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPin([FromForm] double lat, [FromForm] double lng, [FromForm] string description, [FromForm] List<IFormFile> images)
        {
            var imageUrls = new List<string>();
            if (images != null && images.Any())
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }
                        imageUrls.Add($"/images/{uniqueFileName}");
                    }
                }
            }

            var pin = new Pin
            { 
                Lat = lat,
                Lng = lng,
                Description = description,
                ImageUrls = imageUrls
            };

            await _pinRepository.AddPin(pin);
            return CreatedAtAction(nameof(GetPins), new { id = pin.Id }, pin);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePin(Guid id, [FromForm] double lat, [FromForm] double lng, [FromForm] string description, [FromForm] List<string> existingImageUrlsToKeep, [FromForm] List<IFormFile> newImages) // Modified signature
        {
            var existingPin = await _pinRepository.GetPinById(id);
            if (existingPin == null)
            {
                return NotFound();
            }

            // Determine images to delete
            var imagesToDelete = existingPin.ImageUrls.Except(existingImageUrlsToKeep ?? new List<string>()).ToList();
            foreach (var imageUrl in imagesToDelete)
            {
                var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Handle new image uploads
            var newImageUrls = new List<string>();
            if (newImages != null && newImages.Any())
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var image in newImages)
                {
                    if (image.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }
                        newImageUrls.Add($"/images/{uniqueFileName}");
                    }
                }
            }

            // Combine existing images to keep with new image URLs
            existingPin.ImageUrls = (existingImageUrlsToKeep ?? new List<string>()).Concat(newImageUrls).ToList();
            existingPin.Lat = lat;
            existingPin.Lng = lng;
            existingPin.Description = description;

            await _pinRepository.UpdatePin(existingPin);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePin(Guid id)
        {
            var existingPin = await _pinRepository.GetPinById(id);
            if (existingPin == null)
            {
                return NotFound();
            }

            // Delete associated image files
            if (existingPin.ImageUrls != null)
            {
                foreach (var imageUrl in existingPin.ImageUrls)
                {
                    var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            await _pinRepository.DeletePin(id);
            return NoContent();
        }
    }
}