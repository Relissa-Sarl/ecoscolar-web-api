using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class PublicCommentMapper
{
	[MapProperty("Author.UserName", nameof(QuestionResponseDTO.Author))]
	public partial QuestionResponseDTO ToQuestionResponse(PublicComment comment);

	public partial AnswerResponseDTO ToAnswerResponse(PublicComment comment);

	public partial IQueryable<QuestionResponseDTO> ProjectToQuestionResponses(IQueryable<PublicComment> query);
}
