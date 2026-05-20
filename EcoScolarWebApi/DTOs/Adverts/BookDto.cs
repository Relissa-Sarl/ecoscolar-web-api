using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;

namespace EcoScolarWebApi.DTOs.AdvertDtos
{
    public record BookReadDto(long id, string title, string description, decimal price, DateTime publicationDate, DateTime notificationDate, AdvertStatus status, string userId, string sellerPseudo,
        List<string> pictures, Condition condition, long bookCategoryId, string bookCategoryLabel, string isbn, string author, string publisher, string edition, Enums.Language writtenLanguage, decimal? weight = null)
    {
        public static BookReadDto FromEntity(Book entity)
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
                bookCategoryLabel: entity.BookCategories.Description,
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
    /// DTO used for creating new Books adverts, inheriting from ProductCreateDto and adding specific properties related to books, such as category ID, ISBN, author, publisher, and edition.
    /// </summary>
    /// <param name="Title">The title of the Books Adverts</param>
    /// <param name="Description">The description of the Books Adverts</param>
    /// <param name="Price">The price of the Books Adverts</param>
    /// <param name="UserId">The ID of the user who is creating the Books Adverts</param>
    /// <param name="Images">The array of image for the Books Adverts</param>
    /// <param name="Condition">The condition of the Books Adverts</param>
    /// <param name="CategoryId">The ID of the category to which the Books Adverts belongs</param>
    /// <param name="Isbn">The ISBN of the Books Adverts</param>
    /// <param name="Author">The author of the Books Adverts</param>
    /// <param name="Publisher">The publisher of the Books Adverts</param>
    /// <param name="Edition">The edition of the Books Adverts</param>
    public record BookCreateDto(
        string Title, string Description, decimal Price, string UserId, Picture[] Images, Condition Condition,
        long CategoryId, string Isbn, string Author, string Publisher, string Edition, Enums.Language WrittenLanguage
    ) : ProductCreateDto(Title, Description, Price, UserId, Images, Condition, ProductCategoryId: null)
    {
        /// <summary>
        /// Converts the BookCreateDto to a Books entity.
        /// </summary>
        /// <returns>The Books entity</returns>
        public Book ToEntity()
        {
            var Books = new Book();
            this.MapToEntity(Books);
            return Books;
        }

        /// <summary>
        /// Maps the properties of the BookCreateDto to an existing Adverts entity, specifically to a Books entity.
        /// </summary>
        /// <param name="entity">The Adverts entity to map to</param>
        public override void MapToEntity(Advert entity)
        {
            base.MapToEntity(entity);
            if (entity is Book Books)
            {
                Books.Author = Author;
                Books.Publisher = Publisher;
                Books.Edition = Edition;
                Books.ISBN = Isbn;
                Books.BookCategoryId = CategoryId;
                Books.WrittenLanguage = WrittenLanguage;
            }
        }
    }
}
