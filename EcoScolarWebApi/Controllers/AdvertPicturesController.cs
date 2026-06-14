using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcoScolarWebApi.Controllers;

/// <summary>
/// Manages images (pictures) attached to adverts.
/// POST  /api/v1/adverts/{advertId}/pictures       – upload up to 5 images
/// DELETE /api/v1/adverts/{advertId}/pictures/{pictureId} – delete an image
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/adverts/{advertId:long}/pictures")]
[ApiController]
[Authorize]
public class AdvertPicturesController : ControllerBase
{
    private const int MaxPicturesPerAdvert = 5;

    private readonly EcoscolarDbContext _context;
    private readonly IImageStorageService _imageStorage;

    public AdvertPicturesController(EcoscolarDbContext context, IImageStorageService imageStorage)
    {
        _context = context;
        _imageStorage = imageStorage;
    }

    /// <summary>
    /// Upload one or more images for an advert (max 5 total including existing ones).
    /// Only the advert owner or an admin can upload pictures.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25 * 1024 * 1024)] // 5 files × 5 MB
    public async Task<ActionResult<IEnumerable<PictureDto>>> UploadPictures(
        long advertId,
        [FromForm] IFormFileCollection files,
        CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Aucun fichier reçu." });

        var advert = await _context.Products
            .Include(p => p.Pictures)
            .FirstOrDefaultAsync(p => p.AdvertId == advertId, ct);

        if (advert == null)
            return NotFound(new { error = $"Annonce {advertId} introuvable." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (advert.SellerId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var existingCount = advert.Pictures.Count;
        if (existingCount + files.Count > MaxPicturesPerAdvert)
            return BadRequest(new { error = $"Maximum {MaxPicturesPerAdvert} images par annonce (actuellement {existingCount})." });

        var created = new List<PictureDto>();

        foreach (var file in files)
        {
            StoredImageResult stored;
            try
            {
                stored = await _imageStorage.UploadAsync(file, $"adverts/{advertId}", ct);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            var nextOrder = advert.Pictures.Count > 0
                ? advert.Pictures.Max(p => p.SortOrder) + 1
                : 0;

            var picture = new Picture
            {
                Label = stored.ObjectKey,
                ObjectKey = stored.ObjectKey,
                ContentType = stored.ContentType,
                PublicUrl = stored.PublicUrl,
                SortOrder = nextOrder,
                PhysicalItemId = advertId
            };

            _context.Pictures.Add(picture);
            await _context.SaveChangesAsync(ct);

            advert.Pictures.Add(picture);
            created.Add(new PictureDto(picture.PictureId, stored.PublicUrl, picture.SortOrder));
        }

        return Ok(created);
    }

    /// <summary>
    /// Delete a specific image from an advert.
    /// Only the advert owner or an admin can delete pictures.
    /// </summary>
    [HttpDelete("{pictureId:long}")]
    public async Task<IActionResult> DeletePicture(
        long advertId,
        long pictureId,
        CancellationToken ct = default)
    {
        var picture = await _context.Pictures
            .Include(p => p.PhysicalItem)
            .FirstOrDefaultAsync(p => p.PictureId == pictureId && p.PhysicalItemId == advertId, ct);

        if (picture == null)
            return NotFound(new { error = $"Image {pictureId} introuvable pour l'annonce {advertId}." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (picture.PhysicalItem.SellerId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        if (!string.IsNullOrEmpty(picture.ObjectKey))
        {
            try
            {
                await _imageStorage.DeleteAsync(picture.ObjectKey, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MinIO] Failed to delete object {picture.ObjectKey}: {ex.Message}");
            }
        }

        _context.Pictures.Remove(picture);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}
