using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class ReviewMapper
{
	[MapProperty("Reviewed.Nickname", nameof(ReviewResponseDTO.ReviewedNickname))]
	[MapProperty("Reviewer.Nickname", nameof(ReviewResponseDTO.ReviewerNickname))]
	public partial ReviewResponseDTO ToReviewResponseDTO(Review review);
	public partial IEnumerable<ReviewResponseDTO> ToReviewsResponseDTO(IEnumerable<Review> reviews);
	[MapProperty("Reviewed.Nickname", nameof(ReviewResponseDTO.ReviewedNickname))]
	[MapProperty("Reviewer.Nickname", nameof(ReviewResponseDTO.ReviewerNickname))]
	public partial IQueryable<ReviewResponseDTO> ProjectToReviewResponseDTOs(IQueryable<Review> reviews);
}
