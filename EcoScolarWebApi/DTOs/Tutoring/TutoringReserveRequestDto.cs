using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Tutoring;

/// <summary>
/// Booking request for a tutoring advert. The advert id comes from the route;
/// the body only carries the number of hours. The price is computed server-side.
/// </summary>
public class TutoringReserveRequestDto
{
    /// <summary>Number of hours to book. Bounded server-side by the advert's MinHours/MaxHours.</summary>
    [Range(1, 1000, ErrorMessage = "Le nombre d'heures doit être positif.")]
    public int Hours { get; set; }
}
