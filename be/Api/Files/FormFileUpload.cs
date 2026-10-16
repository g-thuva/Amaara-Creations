using be.Application.Abstractions.Storage;

namespace be.Api.Files;
internal static class FormFileUpload
{
    public static MediaUpload ToMediaUpload(this IFormFile file) => new(file.FileName, file.ContentType, file.Length, file.OpenReadStream);
}
