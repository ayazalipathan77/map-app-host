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
using CloudinaryDotNet; // Added for Cloudinary
using CloudinaryDotNet.Actions; // Added for Cloudinary actions

namespace MapApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PinsController : ControllerBase
    {
        private readonly PinRepository _pinRepository;
        private readonly IWebHostEnvironment _env;
        private readonly Cloudinary _cloudinary; // Added for Cloudinary

        public PinsController(PinRepository pinRepository, IWebHostEnvironment env, Cloudinary cloudinary)
        {
            _pinRepository = pinRepository;
            _env = env;
            _cloudinary = cloudinary;
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
                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        //console.WriteLine($"Uploading image: {image.FileName}");
                        // Upload image to Cloudinary
                        // Ensure the Cloudinary instance is properly configured in Program.cs
                        var uploadResult = await UploadImageToCloudinary(image);
                        if (uploadResult != null)
                        {
                            imageUrls.Add(uploadResult.SecureUrl.ToString());
                        }
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

            // Determine images to delete from Cloudinary
            var imagesToDelete = (existingPin.ImageUrls ?? new List<string>()).Except(existingImageUrlsToKeep ?? new List<string>()).ToList();
            foreach (var imageUrl in imagesToDelete)
            {
                await DeleteImageFromCloudinary(imageUrl);
            }

            // Handle new image uploads to Cloudinary
            var newImageUrls = new List<string>();
            if (newImages != null && newImages.Any())
            {
                foreach (var image in newImages)
                {
                    if (image.Length > 0)
                    {
                        var uploadResult = await UploadImageToCloudinary(image);
                        if (uploadResult != null)
                        {
                            newImageUrls.Add(uploadResult.SecureUrl.ToString());
                        }
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

            // Delete associated image files from Cloudinary
            if (existingPin.ImageUrls != null)
            {
                foreach (var imageUrl in existingPin.ImageUrls)
                {
                    await DeleteImageFromCloudinary(imageUrl);
                }
            }

            await _pinRepository.DeletePin(id);
            return NoContent();
        }

        private async Task<ImageUploadResult> UploadImageToCloudinary(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(image.FileName, image.OpenReadStream()),
                PublicId = Guid.NewGuid().ToString(), // Use a GUID as the public ID
                Folder = "mapapp_images" // Optional: organize images in a folder
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult;
        }

        private async Task DeleteImageFromCloudinary(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return;
            }

            try
            {
                // Extract public ID from Cloudinary URL
                // Example URL: https://res.cloudinary.com/didfxynsu/image/upload/v1678888888/mapapp_images/some_public_id.jpg
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;
                var publicIdWithExtension = segments.Last();
                var publicId = Path.GetFileNameWithoutExtension(publicIdWithExtension);

                // If you used a folder, prepend it to the public ID
                if (segments.Length > 2 && segments[segments.Length - 2].Trim('/') == "mapapp_images")
                {
                    publicId = "mapapp_images/" + publicId;
                }

                var deletionParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deletionParams);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image from Cloudinary: {ex.Message}");
            }
        }
    }
}