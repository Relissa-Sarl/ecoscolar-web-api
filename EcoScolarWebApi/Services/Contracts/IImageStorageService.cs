namespace EcoScolarWebApi.Services.Contracts;

public record StoredImageResult(string ObjectKey, string PublicUrl, string ContentType);

public interface IImageStorageService
{
    /// <summary>Upload a validated image file and return its storage metadata.</summary>
    Task<StoredImageResult> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>Delete a stored image by its object key.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
