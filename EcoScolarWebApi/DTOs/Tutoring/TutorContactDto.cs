namespace EcoScolarWebApi.DTOs.Tutoring;

public record TutorContactDto(
    string Name,
    string? PhoneNumber,
    string? Email
);
