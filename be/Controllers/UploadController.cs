using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Security;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/upload")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly long _maxFileSize;
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedImageContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        public UploadController(ILogger<UploadController> logger, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _logger = logger;
            _environment = environment;
            _maxFileSize = configuration.GetValue<long?>("Uploads:MaxFileSizeBytes") ?? 5 * 1024 * 1024;
        }

        // POST: api/v1/upload/avatar - Upload user avatar
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
                if (file.Length > _maxFileSize)
                {
                    return BadRequest(new { message = $"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024}MB" });
                }

                var safeOriginalFileName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(safeOriginalFileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new { message = $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageExtensions)}" });
                }

                if (!AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    return BadRequest(new { message = "Invalid image content type" });
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
                await using (var stream = new FileStream(filePath, FileMode.CreateNew))
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

