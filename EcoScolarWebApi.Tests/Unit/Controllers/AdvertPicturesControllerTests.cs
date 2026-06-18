using EcoScolarWebApi.Controllers;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Security.Claims;
using Xunit;

namespace EcoScolarWebApi.Tests.Unit.Controllers;

public class AdvertPicturesControllerTests : IDisposable
{
	private readonly EcoscolarDbContext _context;
	private readonly IImageStorageService _imageStorageMock;
	private readonly AdvertPicturesController _controller;

	public AdvertPicturesControllerTests()
	{
		var options = new DbContextOptionsBuilder<EcoscolarDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		_context = new EcoscolarDbContext(options);
		_imageStorageMock = Substitute.For<IImageStorageService>();

		_controller = new AdvertPicturesController(_context, _imageStorageMock);
	}

	public void Dispose()
	{
		_context.Database.EnsureDeleted();
		_context.Dispose();
		GC.SuppressFinalize(this);
	}

	private void SetUser(string userId, bool isAdmin = false)
	{
		var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
		if (isAdmin) claims.Add(new(ClaimTypes.Role, "Admin"));
		_controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
			}
		};
	}

	private static IFormFileCollection CreateMockFiles(int count)
	{
		var files = new FormFileCollection();
		for (int i = 0; i < count; i++)
		{
			var stream = new MemoryStream(new byte[] { 0xFF, 0xD8 });
			files.Add(new FormFile(stream, 0, stream.Length, $"file{i}", $"image{i}.jpg")
			{
				Headers = new HeaderDictionary(),
				ContentType = "image/jpeg"
			});
		}
		return files;
	}

	#region UploadPictures Tests

	[Fact]
	public async Task UploadPictures_ShouldReturnBadRequest_WhenNoFilesProvided()
	{
		// Arrange
		SetUser("owner-1");
		var files = new FormFileCollection();

		// Act
		var result = await _controller.UploadPictures(1L, files);

		// Assert
		result.Result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task UploadPictures_ShouldReturnNotFound_WhenAdvertDoesNotExist()
	{
		// Arrange
		SetUser("owner-1");
		var files = CreateMockFiles(1);

		// Act
		var result = await _controller.UploadPictures(999L, files);

		// Assert
		result.Result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task UploadPictures_ShouldReturnForbid_WhenUserIsNotOwnerOrAdmin()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 1,
			Title = "Book",
			Description = "Desc",
			Price = 10m,
			SellerId = "owner-1",
			ISBN = "123",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		SetUser("intruder");
		var files = CreateMockFiles(1);

		// Act
		var result = await _controller.UploadPictures(1L, files);

		// Assert
		result.Result.Should().BeOfType<ForbidResult>();
	}

	[Fact]
	public async Task UploadPictures_ShouldReturnBadRequest_WhenExceedingMaxPictures()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 2,
			Title = "Book with pics",
			Description = "Has 4 pics already",
			Price = 10m,
			SellerId = "owner-2",
			ISBN = "456",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		// Add 4 existing pictures
		for (int i = 0; i < 4; i++)
		{
			_context.Pictures.Add(new Picture
			{
				Label = $"pic{i}",
				PhysicalItemId = 2,
				SortOrder = i
			});
		}
		await _context.SaveChangesAsync();

		SetUser("owner-2");
		var files = CreateMockFiles(2); // 4 + 2 = 6 > 5

		// Act
		var result = await _controller.UploadPictures(2L, files);

		// Assert
		result.Result.Should().BeOfType<BadRequestObjectResult>();
	}

	[Fact]
	public async Task UploadPictures_ShouldSucceed_WhenOwnerUploadsValidFiles()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 3,
			Title = "Book",
			Description = "No pics yet",
			Price = 10m,
			SellerId = "owner-3",
			ISBN = "789",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		_imageStorageMock.UploadAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new StoredImageResult("adverts/3/img.jpg", "http://cdn/img.jpg", "image/jpeg"));

		SetUser("owner-3");
		var files = CreateMockFiles(1);

		// Act
		var result = await _controller.UploadPictures(3L, files);

		// Assert
		result.Result.Should().BeOfType<OkObjectResult>();
	}

	[Fact]
	public async Task UploadPictures_ShouldSucceed_WhenAdminUploads()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 4,
			Title = "Admin Book",
			Description = "Admin can upload",
			Price = 10m,
			SellerId = "other-user",
			ISBN = "000",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		_imageStorageMock.UploadAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(new StoredImageResult("adverts/4/img.jpg", "http://cdn/img.jpg", "image/jpeg"));

		SetUser("admin-user", isAdmin: true);
		var files = CreateMockFiles(1);

		// Act
		var result = await _controller.UploadPictures(4L, files);

		// Assert
		result.Result.Should().BeOfType<OkObjectResult>();
	}

	#endregion

	#region DeletePicture Tests

	[Fact]
	public async Task DeletePicture_ShouldReturnNotFound_WhenPictureDoesNotExist()
	{
		// Arrange
		SetUser("owner-1");

		// Act
		var result = await _controller.DeletePicture(1L, 999L);

		// Assert
		result.Should().BeOfType<NotFoundObjectResult>();
	}

	[Fact]
	public async Task DeletePicture_ShouldReturnForbid_WhenUserIsNotOwner()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 10,
			Title = "Book",
			Description = "Desc",
			Price = 10m,
			SellerId = "owner-10",
			ISBN = "123",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var picture = new Picture
		{
			PictureId = 10,
			Label = "test.jpg",
			ObjectKey = "adverts/10/test.jpg",
			PhysicalItemId = 10,
			SortOrder = 0
		};
		_context.Pictures.Add(picture);
		await _context.SaveChangesAsync();

		SetUser("intruder");

		// Act
		var result = await _controller.DeletePicture(10L, 10L);

		// Assert
		result.Should().BeOfType<ForbidResult>();
	}

	[Fact]
	public async Task DeletePicture_ShouldReturnNoContent_WhenOwnerDeletes()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 11,
			Title = "Book",
			Description = "Desc",
			Price = 10m,
			SellerId = "owner-11",
			ISBN = "456",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var picture = new Picture
		{
			PictureId = 11,
			Label = "test.jpg",
			ObjectKey = "adverts/11/test.jpg",
			PhysicalItemId = 11,
			SortOrder = 0
		};
		_context.Pictures.Add(picture);
		await _context.SaveChangesAsync();

		SetUser("owner-11");

		// Act
		var result = await _controller.DeletePicture(11L, 11L);

		// Assert
		result.Should().BeOfType<NoContentResult>();
		await _imageStorageMock.Received(1).DeleteAsync("adverts/11/test.jpg", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeletePicture_ShouldRemovePictureFromDb()
	{
		// Arrange
		var book = new Book
		{
			AdvertId = 12,
			Title = "Book",
			Description = "Desc",
			Price = 10m,
			SellerId = "owner-12",
			ISBN = "789",
			Author = "Author",
			Publisher = "Pub",
			Edition = "1st",
			WrittenLanguage = LanguageEnum.FR,
			Status = AdvertStatus.ACTIVE
		};
		_context.Products.Add(book);
		await _context.SaveChangesAsync();

		var picture = new Picture
		{
			PictureId = 12,
			Label = "del.jpg",
			ObjectKey = "adverts/12/del.jpg",
			PhysicalItemId = 12,
			SortOrder = 0
		};
		_context.Pictures.Add(picture);
		await _context.SaveChangesAsync();

		SetUser("owner-12");

		// Act
		await _controller.DeletePicture(12L, 12L);

		// Assert
		var picInDb = await _context.Pictures.FindAsync(12L);
		picInDb.Should().BeNull();
	}

	#endregion
}
