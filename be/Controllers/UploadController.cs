using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IWebHostEnvironment _environment;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public UploadController(ILogger<UploadController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        // POST: api/upload/product-image - Upload product image
        [HttpPost("product-image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UploadProductImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { message = $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB" });
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new { message = $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageExtensions)}" });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return file URL
                var fileUrl = $"/uploads/products/{fileName}";

                return Ok(new
                {
                    message = "Product image uploaded successfully",
                    fileUrl = fileUrl,
                    fileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image");
                return StatusCode(500, new { message = "An error occurred while uploading the image" });
            }
        }

        // POST: api/upload/avatar - Upload user avatar
        [HttpPost("avatar")]
        [Authorize]
        public async Task<ActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { message = $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB" });
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new { message = $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageExtensions)}" });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename with user ID prefix
                var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return file URL
                var fileUrl = $"/uploads/avatars/{fileName}";

                return Ok(new
                {
                    message = "Avatar uploaded successfully",
                    fileUrl = fileUrl,
                    fileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return StatusCode(500, new { message = "An error occurred while uploading the avatar" });
            }
        }
    }
}

