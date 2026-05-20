using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs.Advert;
using EcoscolarWebApi.Utils.Enums;

namespace EcoScolarWebApi.Utils.DTOs.Advert
{
    public record ServiceReadDto(long id, string title, string description, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo,
        long subjectId, string subjectLabel, long schoolGradeId, string schoolGradeLabel, Language teachingLanguage, string studyLevel)
    {
        public static ServiceReadDto FromEntity(AdvertServices entity)
        {
            return new ServiceReadDto(
                id: entity.AdvertId,
                title: entity.Title,
                description: entity.Description,
                price: entity.Price,
                publicationDate: entity.CreatedAt,
                notificationDate: entity.NotificationDate,
                status: entity.Status,
                userId: entity.UserId,
                sellerPseudo: entity.User.UserName ?? "Anonyme",
                subjectId: entity.SubjectId,
                subjectLabel: entity.Subject.Name,
                schoolGradeId: entity.SchoolGradeId,
                schoolGradeLabel: entity.SchoolGrade.Name,
                teachingLanguage: entity.TeachingLanguage,
                studyLevel: entity.StudyLevel
            );
        }
    }

    /// <summary>
    /// DTO used for creating new service adverts, inheriting from AdvertBaseCreateDto and adding specific properties related to services, such as subject ID, school level ID, teaching language, and specific study level.
    /// </summary>
    /// <param name="Title">The title of the service advert</param>
    /// <param name="Description">The description of the service advert</param>
    /// <param name="Price">The price of the service advert</param>
    /// <param name="UserId">The ID of the user who is creating the service advert</param>
    /// <param name="SubjectId">The ID of the subject related to the service advert</param>
    /// <param name="SchoolLevelId">The ID of the school level related to the service advert</param>
    /// <param name="TeachingLanguage">The language in which the service will be taught</param>
    /// <param name="SpecificStudyLevel">The specific study level related to the service advert</param>
    public record ServiceCreateDto(string Title, string Description, decimal Price, string UserId, long SubjectId, long SchoolLevelId, Language TeachingLanguage, string SpecificStudyLevel)
        : AdvertCreateDto(Title, Description, Price, UserId)
    {
        /// <summary>
        /// Converts the ServiceCreateDto to an AdvertServices entity.
        /// </summary>
        /// <returns>The AdvertServices entity</returns>
        public AdvertServices ToEntity()
        {
            var service = new AdvertServices();
            this.MapToEntity(service);
            return service;
        }

        /// <summary>
        /// Maps the properties of the ServiceCreateDto to an existing Adverts entity, specifically to an AdvertServices entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public override void MapToEntity(Adverts entity)
        {
            base.MapToEntity(entity);
            if (entity is AdvertServices service)
            {
                service.SubjectId = SubjectId;
                service.SchoolGradeId = SchoolLevelId;
                service.TeachingLanguage = TeachingLanguage;
                service.StudyLevel = SpecificStudyLevel;
            }
        }
    }
}
