using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.Enums;

namespace EcoscolarWebApi.Utils.DTOs.Advert
{
    public record BookReadDto(long id, string title, string description, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo,
        List<string> pictures, Condition condition, long bookCategoryId, string bookCategoryLabel, string isbn, string author, string publisher, string edition, Language writtenLanguage, decimal? weight = null)
    {
        public static BookReadDto FromEntity(Books entity)
        {
            return new BookReadDto(
                id: entity.AdvertId,
                title: entity.Title,
                description: entity.Description,
                price: entity.Price,
                publicationDate: entity.CreatedAt,
                notificationDate: entity.NotificationDate,
                status: entity.Status,
                userId: entity.UserId,
                sellerPseudo: entity.User?.UserName ?? "Anonyme",
                condition: entity.Condition,
                weight: entity.Weight,
                bookCategoryId: entity.BookCategoryId,
                bookCategoryLabel: entity.BookCategory.Description,
                isbn: entity.ISBN,
                author: entity.Author,
                publisher: entity.Publisher,
                edition: entity.Edition,
                writtenLanguage: entity.WrittenLanguage,
                pictures: entity.Pictures?.Select(p => p.Label).ToList() ?? new List<string>()
            );
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
    ) : ProductCreateDto(Title, Description, Price, UserId, Images, Condition, ProductCategoryId: null)
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
