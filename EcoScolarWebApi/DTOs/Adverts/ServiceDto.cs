using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.Adverts;

public record ServiceReadDto(long Id, string Title, string Description, decimal Price, DateTime PublicationDate, DateTime NotificationDate, AdvertStatus Status, string UserId, string SellerPseudo,
	long SubjectId, string SubjectLabel, long SchoolGradeId, string SchoolGradeLabel, Enums.LanguageEnum TeachingLanguage, string StudyLevel)
{
	public static ServiceReadDto FromEntity(TutoringAdvert entity)
	{
		return new ServiceReadDto(
			Id: entity.AdvertId,
			Title: entity.Title,
			Description: entity.Description,
			Price: entity.Price,
			PublicationDate: entity.CreatedAt,
			NotificationDate: entity.NotificationDate,
			Status: entity.Status,
			UserId: entity.SellerId,
			SellerPseudo: entity.Seller?.UserName ?? "Anonyme",
			SubjectId: entity.SubjectId,
			SubjectLabel: entity.Subject.Name,
			SchoolGradeId: entity.SchoolGradeId,
			SchoolGradeLabel: entity.SchoolGrade.Name,
			TeachingLanguage: entity.TeachingLanguage,
			StudyLevel: entity.StudyLevel
		);
	}
}

/// <summary>
/// DTO used for creating new service adverts, inheriting from AdvertBaseCreateDto and adding specific properties related to services, such as Subjects ID, school level ID, teaching language, and specific study level.
/// </summary>
/// <param name="Title">The title of the service PhysicalItem</param>
/// <param name="Description">The description of the service PhysicalItem</param>
/// <param name="Price">The price of the service PhysicalItem</param>
/// <param name="UserId">The ID of the user who is creating the service PhysicalItem</param>
/// <param name="SubjectId">The ID of the Subjects related to the service PhysicalItem</param>
/// <param name="SchoolLevelId">The ID of the school level related to the service PhysicalItem</param>
/// <param name="TeachingLanguage">The language in which the service will be taught</param>
/// <param name="SpecificStudyLevel">The specific study level related to the service PhysicalItem</param>
public record ServiceCreateDto(string Title, string Description, decimal Price, string UserId, long SubjectId, long SchoolLevelId, Enums.LanguageEnum TeachingLanguage, string SpecificStudyLevel)
	: AdvertCreateDto(Title, Description, Price, UserId)
{
	/// <summary>
	/// Converts the ServiceCreateDto to an AdvertServices entity.
	/// </summary>
	/// <returns>The AdvertServices entity</returns>
	public new TutoringAdvert ToEntity()
	{
		var service = new TutoringAdvert();
		this.MapToEntity(service);
		return service;
	}

	/// <summary>
	/// Maps the properties of the ServiceCreateDto to an existing PhysicalItem entity, specifically to an AdvertServices entity.
	/// </summary>
	/// <param name="entity">The PhysicalItem entity to map to</param>
	public override void MapToEntity(Advert entity)
	{
		base.MapToEntity(entity);
		if (entity is TutoringAdvert service)
		{
			service.SubjectId = SubjectId;
			service.SchoolGradeId = SchoolLevelId;
			service.TeachingLanguage = TeachingLanguage;
			service.StudyLevel = SpecificStudyLevel;
		}
	}
}
