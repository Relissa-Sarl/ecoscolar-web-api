using System.ComponentModel.DataAnnotations;

namespace EcoScolarWebApi.DTOs.Adverts;

// DTO to read a question and its answer (if any) for an advert
public class QuestionResponseDTO
{
    public int CommentId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Answer { get; set; } = string.Empty;
    public DateTime? AnsweredAt { get; set; }
}

// DTO to read an answer to a question for an advert
public class AnswerResponseDTO
{
    public string Answer { get; set; } = string.Empty;
    public DateTime? AnsweredAt { get; set; }
}

// DTO to create a new question for an advert
public record QuestionRequestDTO([Required] string Content);

// DTO to create a new answer to a question for an advert
public record AnswerRequestDTO([Required] string Content);