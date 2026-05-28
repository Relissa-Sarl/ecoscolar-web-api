using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.ReferenceData;

// DTO to create or update a subject
public record SubjectRequest(
    [Required][StringLength(100)] string Name,
    [Required][StringLength(100)] string Code
);

// DTO to return subject data in responses
public record SubjectResponse(
    int SubjectId,
    string Name,
    string Code
);
