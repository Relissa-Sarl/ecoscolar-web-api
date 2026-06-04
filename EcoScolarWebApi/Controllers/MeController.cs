using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoScolarWebApi.DTOs;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using System.Security.Claims;

namespace EcoScolarWebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Ces routes nécessitent un utilisateur connecté
public class MeController : ControllerBase
{
    // L'injection de dépendances (services) se fera ici plus tard

    [HttpGet("purchases")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseReadDto>), StatusCodes.Status200OK)]
    public IActionResult GetMyPurchases()
    {
        // TODO: Implémenter la vraie logique avec le service pour récupérer les achats de l'utilisateur
        // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Données mockées pour le frontend
        var mockPurchases = new List<PurchaseReadDto>
        {
            new PurchaseReadDto(
                Id: "pur_101",
                AdvertId: "adv_500",
                AdvertTitle: "Calculatrice Casio Graph 90+E",
                Price: 45.00m,
                PurchaseDate: DateTime.UtcNow.AddDays(-2),
                Status: "completed",
                ImageUrl: "https://example.com/images/casio.jpg",
                SellerName: "JeanDupont"
            ),
            new PurchaseReadDto(
                Id: "pur_102",
                AdvertId: "adv_501",
                AdvertTitle: "Livre Mathématiques TS",
                Price: 15.50m,
                PurchaseDate: DateTime.UtcNow.AddDays(-5),
                Status: "pending",
                ImageUrl: null,
                SellerName: "MarieClaire"
            )
        };

        return Ok(mockPurchases);
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(IEnumerable<AdvertReadDto>), StatusCodes.Status200OK)]
    public IActionResult GetMySales()
    {
        // TODO: Implémenter la vraie logique avec le service pour récupérer les ventes de l'utilisateur
        // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Données mockées pour le frontend (en utilisant AdvertReadDto)
        var mockSales = new List<AdvertReadDto>
        {
            new AdvertReadDto(
                Id: 201,
                Type: "PRODUCT",
                Title: "Sac à dos Eastpak",
                Price: 20.00m,
                PublicationDate: DateTime.UtcNow.AddDays(-10),
                NotificationDate: DateTime.UtcNow.AddMonths(3),
                Status: AdvertStatus.ACTIVE,
                UserId: "user_777",
                SellerPseudo: "MoiMeme",
                PrimaryImage: "https://example.com/images/sac.jpg",
                BuyerName: null // Pas encore vendu
            ),
            new AdvertReadDto(
                Id: 202,
                Type: "BOOK",
                Title: "Dictionnaire Le Robert",
                Price: 10.00m,
                PublicationDate: DateTime.UtcNow.AddDays(-20),
                NotificationDate: DateTime.UtcNow.AddMonths(3),
                Status: AdvertStatus.SOLD, // Vendu
                UserId: "user_777",
                SellerPseudo: "MoiMeme",
                PrimaryImage: null,
                BuyerName: "Alice Dubois" // Le nom de l'acheteur
            )
        };

        return Ok(mockSales);
    }
}