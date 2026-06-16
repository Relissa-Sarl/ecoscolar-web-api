using EcoScolarWebApi.Helpers;
using EcoScolarWebApi.Services.Contracts;
using Minio;
using Minio.DataModel.Args;

namespace EcoScolarWebApi.Services;

public class MinioImageStorageService : IImageStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private static readonly Dictionary<string, string> MimeToExtension = new()
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };

    private readonly IMinioClient _minio;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;

    public MinioImageStorageService(IMinioClient minio, IConfiguration config)
    {
        _minio = minio;
        _bucket = config["Minio:Bucket"] ?? "ecoscolar-adverts";
        _publicBaseUrl = config["Minio:PublicBaseUrl"]?.TrimEnd('/') ?? "http://localhost:9000/ecoscolar-adverts";
    }

    public async Task<StoredImageResult> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var contentType = file.ContentType?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedMimeTypes.Contains(contentType))
            throw new InvalidOperationException($"Type de fichier non autorisé : {contentType}. Formats acceptés : JPEG, PNG, WebP.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"Le fichier dépasse la taille maximale autorisée de 5 Mo.");

        if (file.Length == 0)
            throw new InvalidOperationException("Le fichier est vide.");

        var ext = MimeToExtension[contentType];
        var fileName = $"{NanoId.Generate()}.{ext}";
        var objectKey = $"{folder.TrimEnd('/')}/{fileName}";

        await EnsureBucketExistsAsync(ct);

        using var stream = file.OpenReadStream();
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(putArgs, ct);

        var publicUrl = $"{_publicBaseUrl}/{objectKey}";
        return new StoredImageResult(objectKey, publicUrl, contentType);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey);

        await _minio.RemoveObjectAsync(removeArgs, ct);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucket), ct);

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucket), ct);

            // Set the bucket to public read so images are accessible without signing
            var policy = $$"""
                {
                  "Version": "2012-10-17",
                  "Statement": [
                    {
                      "Effect": "Allow",
                      "Principal": {"AWS": ["*"]},
                      "Action": ["s3:GetObject"],
                      "Resource": ["arn:aws:s3:::{{_bucket}}/*"]
                    }
                  ]
                }
                """;

            await _minio.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(_bucket).WithPolicy(policy), ct);
        }
    }
}
