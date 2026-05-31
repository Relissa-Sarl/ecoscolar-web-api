using EcoScolarWebApi.DTOs.Reviews;
using EcoScolarWebApi.Models;
using Riok.Mapperly.Abstractions;

namespace EcoScolarWebApi.Mappers;

[Mapper]
public partial class ReviewMapper
{
	public partial ReviewResponseDTO ToReviewResponseDTO(Review review);
}
