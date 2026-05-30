using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class PublicCommentMapper
{
	[MapProperty(nameof(PublicComment.AuthorId), nameof(QuestionResponseDTO.AuthorId))]
	[MapProperty("Author.UserName", nameof(QuestionResponseDTO.Author))]
	public partial QuestionResponseDTO ToQuestionResponse(PublicComment comment);

	[MapProperty("Advert.Seller.UserName", nameof(AnswerResponseDTO.Seller))]
	[MapProperty(nameof(PublicComment.Answer), nameof(AnswerResponseDTO.Answer))]
	[MapProperty(nameof(PublicComment.AnsweredAt), nameof(AnswerResponseDTO.AnsweredAt))]
	public partial AnswerResponseDTO ToAnswerResponse(PublicComment comment);
}
