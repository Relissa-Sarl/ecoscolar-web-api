using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.Enums;
using Microsoft.AspNetCore.Identity;
using System.Runtime.InteropServices;

namespace EcoscolarWebApi.Utils.DTOs.Advert
{
    public record AdvertReadDto(long id, string type, string title, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo, string? primaryImage)
    {
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
    public record AdvertBaseCreateDto(string Title, string Description, decimal Price, string UserId)
    {
        public Adverts ToEntity()
        {
            var advert = new Adverts();
            this.MapToEntity(advert);
            return advert;
        }

        public virtual void MapToEntity(Adverts entity)
        {
            entity.Title = Title;
            entity.Description = Description;
            entity.Price = Price;
            entity.Status = AdvertStatus.ACTIVE;
            entity.UserId = UserId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.NotificationDate = DateTime.UtcNow.AddMonths(3);
        }
    }

    public record ServiceCreateDto(string Title, string Description, decimal Price, string UserId, long SubjectId, long SchoolLevelId, Language TeachingLanguage, string SpecificStudyLevel)
        : AdvertBaseCreateDto(Title, Description, Price, UserId)
    {
        public AdvertServices ToEntity()
        {
            var service = new AdvertServices();
            this.MapToEntity(service);
            return service;
        }

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

    public record ProductCreateDto(string Title, string Description, decimal Price, string UserId, string[] Images, Condition Condition)
        : AdvertBaseCreateDto(Title, Description, Price, UserId)
    {
        public PhysicalItems ToEntity()
        {
            var product = new PhysicalItems();
            this.MapToEntity(product);
            return product;
        }

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

    public record BookCreateDto(
        string Title, string Description, decimal Price, string UserId, string[] Images, Condition Condition,
        long CategoryId, string Isbn, string Author, string Publisher, string Edition
    ) : ProductCreateDto(Title, Description, Price, UserId, Images, Condition)
    {
        public Books ToEntity()
        {
            var book = new Books();
            this.MapToEntity(book);
            return book;
        }

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
            }
        }
    }
}
