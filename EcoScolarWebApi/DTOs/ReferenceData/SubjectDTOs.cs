using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.ReferenceData;

// DTO to create or update a subject
public record SubjectRequestDTO(
    [Required][StringLength(100)] string Name,
    [Required][StringLength(100)] string Code,
    [Required][StringLength(100)] string NameFr,
    [Required][StringLength(100)] string NameDe,
    [Required][StringLength(100)] string NameIt
);

// DTO to return subject data in responses
public record SubjectResponseDTO(
    int SubjectId,
    string Name,
    string NameFr,
    string NameDe,
    string NameIt,
    string Code
);
