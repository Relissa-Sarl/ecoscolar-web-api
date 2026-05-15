using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.Enums;
using Microsoft.AspNetCore.Identity;
using System.Runtime.InteropServices;

namespace EcoscolarWebApi.Utils.DTOs.Advert
{
    /// <summary>
    /// DTO used to return advert information in a simplified form, suitable for listing and basic display purposes. 
    /// It includes essential details such as the advert's ID, type, title, price, publication date, notification date, status, user ID of the seller, seller's pseudo (username), 
    /// and the URL of the primary image if available. 
    /// This DTO is designed to provide a concise overview of an advert without exposing all the underlying details of the entity.
    /// </summary>
    /// <param name="id">The ID of the advert</param>
    /// <param name="type">The type of the advert</param>
    /// <param name="title">The title of the advert</param>
    /// <param name="price">The price of the advert</param>
    /// <param name="publicationDate">The date when the advert was published</param>
    /// <param name="notificationDate">The date when the advert was notified</param>
    /// <param name="status">The status of the advert</param>
    /// <param name="userId">The ID of the user who created the advert</param>
    /// <param name="sellerPseudo">The pseudo (username) of the seller</param>
    /// <param name="primaryImage">The URL of the primary image of the advert</param>
    public record AdvertReadDto(long id, string type, string title, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo, string? primaryImage)
    {
        /// <summary>
        /// Factory method to create an AdvertReadDto from an Adverts entity.
        /// It determines the type of advert based on the specific subclass of Adverts (Books, PhysicalItems, AdvertServices) and extracts the primary image if available.
        /// </summary>
        /// <param name="entity">The Adverts entity to convert</param>
        /// <returns>The AdvertReadDto instance</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static AdvertReadDto FromEntity(Models.Adverts entity)
        {
            string type = entity switch
            {
                Models.Books => "BOOK",
                Models.PhysicalItems => "PRODUCT",
                Models.AdvertServices => "SERVICE",
                _ => throw new InvalidOperationException("Unknown advert type")
            };

            string? primaryImage = (entity as Models.PhysicalItems)?.Pictures?.FirstOrDefault()?.Label;

            return new AdvertReadDto(
                id: entity.AdvertId,
                type: type,
                title:entity.Title,
                price: entity.Price,
                publicationDate: entity.CreatedAt,
                notificationDate: entity.NotificationDate,
                status: entity.Status,
                userId: entity.UserId,
                sellerPseudo: entity.User?.UserName ?? "Anonyme",
                primaryImage: primaryImage
            );
        }
    }

    /// <summary>
    /// DTO used for creating new adverts, serving as a base class for specific advert types like services and products.
    /// </summary>
    /// <param name="Title">The title of the advert</param>
    /// <param name="Description">The description of the advert</param>
    /// <param name="Price">The price of the advert</param>
    /// <param name="UserId">The ID of the user who is creating the advert</param>
    public record AdvertBaseCreateDto(string Title, string Description, decimal Price, string UserId)
    {
        /// <summary>
        /// Converts the AdvertBaseCreateDto to an Adverts entity. 
        /// This method initializes the common properties of the advert, such as title, description, price, user ID, status, creation date, and notification date.
        /// </summary>
        /// <returns>The Adverts entity</returns>
        public Adverts ToEntity()
        {
            var advert = new Adverts();
            this.MapToEntity(advert);
            return advert;
        }

        /// <summary>
        /// Maps the properties of the AdvertBaseCreateDto to an existing Adverts entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public virtual void MapToEntity(Adverts entity)
        {
            entity.Title = Title;
            entity.Description = Description;
            entity.Price = Price;
            entity.Status = AdvertStatus.ACTIVE;
            entity.UserId = UserId;
            entity.NotificationDate = DateTime.UtcNow.AddMonths(3);
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
        : AdvertBaseCreateDto(Title, Description, Price, UserId)
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

    /// <summary>
    /// DTO used for creating new product adverts, inheriting from AdvertBaseCreateDto and adding specific properties related to products, such as an array of image URLs and the condition of the product.
    /// </summary>
    /// <param name="Title">The title of the product advert</param>
    /// <param name="Description">The description of the product advert</param>
    /// <param name="Price">The price of the product advert</param>
    /// <param name="UserId">The ID of the user who is creating the product advert</param>
    /// <param name="Images">The array of image URLs for the product advert</param>
    /// <param name="Condition">The condition of the product advert</param>
    public record ProductCreateDto(string Title, string Description, decimal Price, string UserId, string[] Images, Condition Condition)
        : AdvertBaseCreateDto(Title, Description, Price, UserId)
    {
        /// <summary>
        /// Converts the ProductCreateDto to a PhysicalItems entity.
        /// </summary>
        /// <returns>The PhysicalItems entity</returns>
        public PhysicalItems ToEntity()
        {
            var product = new PhysicalItems();
            this.MapToEntity(product);
            return product;
        }

        /// <summary>
        /// Maps the properties of the ProductCreateDto to an existing Adverts entity, specifically to a PhysicalItems entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public override void MapToEntity(Adverts entity)
        {
            base.MapToEntity(entity);
            if (entity is PhysicalItems product)
            {
                product.Condition = Condition;
                product.Pictures = Images.Select(url => new Pictures { Label = url }).ToList();
            }
        }
    }

    /// <summary>
    /// DTO used for creating new book adverts, inheriting from ProductCreateDto and adding specific properties related to books, such as category ID, ISBN, author, publisher, and edition.
    /// </summary>
    /// <param name="Title">The title of the book advert</param>
    /// <param name="Description">The description of the book advert</param>
    /// <param name="Price">The price of the book advert</param>
    /// <param name="UserId">The ID of the user who is creating the book advert</param>
    /// <param name="Images">The array of image URLs for the book advert</param>
    /// <param name="Condition">The condition of the book advert</param>
    /// <param name="CategoryId">The ID of the category to which the book advert belongs</param>
    /// <param name="Isbn">The ISBN of the book advert</param>
    /// <param name="Author">The author of the book advert</param>
    /// <param name="Publisher">The publisher of the book advert</param>
    /// <param name="Edition">The edition of the book advert</param>
    public record BookCreateDto(
        string Title, string Description, decimal Price, string UserId, string[] Images, Condition Condition,
        long CategoryId, string Isbn, string Author, string Publisher, string Edition, Language WrittenLanguage
    ) : ProductCreateDto(Title, Description, Price, UserId, Images, Condition)
    {
        /// <summary>
        /// Converts the BookCreateDto to a Books entity.
        /// </summary>
        /// <returns>The Books entity</returns>
        public Books ToEntity()
        {
            var book = new Books();
            this.MapToEntity(book);
            return book;
        }

        /// <summary>
        /// Maps the properties of the BookCreateDto to an existing Adverts entity, specifically to a Books entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public override void MapToEntity(Adverts entity)
        {
            base.MapToEntity(entity);
            if (entity is Books book)
            {
                book.Author = Author;
                book.Publisher = Publisher;
                book.Edition = Edition;
                book.ISBN = Isbn;
                book.BookCategoryId = CategoryId;
                book.WrittenLanguage = WrittenLanguage;
            }
        }
    }
}
