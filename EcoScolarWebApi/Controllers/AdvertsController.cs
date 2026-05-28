using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Adverts;
using EcoScolarWebApi.Enums;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AdvertsController : ControllerBase
{
	private readonly IAdvertSearchService _advertSearchService;
	private readonly EcoscolarDbContext _context;                                           // Database context for accessing the database
	private bool AdvertExists(long id) => _context.Adverts.Any(e => e.AdvertId == id);      // Helper method to check if an PhysicalItem with the specified ID exists in the database

	/// <summary>
	/// AdvertsController constructor
	/// </summary>
	/// <param name="context">The database context</param>
	/// <param name="advertSearchService">The PhysicalItem search service</param>
	public AdvertsController(EcoscolarDbContext context, IAdvertSearchService advertSearchService)
	{
		_context = context;
		_advertSearchService = advertSearchService;
	}

	#region GET METHODS

	/// <summary>
	/// Get all adverts whatever their type is (Books, product or service)
	/// 
	/// GET: AdvertsController
	/// Url: /api/v1/adverts
	/// </summary>
	/// <returns>List of all formatted adverts</returns>
	[HttpGet]
	public async Task<ActionResult<IEnumerable<AdvertDetailDto>>> Index()
	{
		IEnumerable<Advert> adverts;
		try
		{
			adverts = await _context.Adverts
				.Include(a => a.Seller)
				.ToListAsync();
			List<long> physicalItemIds = adverts.OfType<PhysicalItem>()
				.Select(item => item.AdvertId)
				.ToList();
			if (physicalItemIds.Any())
			{
				await _context.Pictures
					.Where(Pictures => physicalItemIds.Contains(Pictures.PhysicalItemId))
					.LoadAsync();
			}

		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		return Ok(adverts.Select(AdvertReadDto.FromEntity));
	}

	/// <summary>
	/// Get all books adverts
	/// 
	/// GET: AdvertsController/GetBooks
	/// Url: /api/v1/adverts/books
	/// </summary>
	/// <returns>List of formatted Books adverts</returns>
	[HttpGet("books")]
	public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetBooks()
	{
		IEnumerable<Book> books;
		try
		{
			books = await _context.Books
				.Include(p => p.Seller)
				.Include(p => p.Pictures)
				.ToListAsync();

		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		return Ok(books.Select(AdvertReadDto.FromEntity));
	}

	/// <summary>
	/// Get all products adverts
	/// 
	/// GET: AdvertsController/GetProducts
	/// Url: /api/v1/adverts/products
	/// </summary>
	/// <param name="categoryId">The category ID to filter products by</param>
	/// <param name="maxPrice">The maximum price to filter products by</param>
	/// <returns>List of formatted product adverts</returns>
	[HttpGet("products")]
	public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetProducts(
			[FromQuery] long? categoryId = null,
			[FromQuery] decimal? maxPrice = null)
	{
		IEnumerable<PhysicalItem> products;
		try
		{
			var query = _context.Products
				.Include(p => p.Seller)
				.Include(p => p.Pictures)
				.Where(p => !_context.Set<Book>().Any(b => b.AdvertId == p.AdvertId));
			if (categoryId.HasValue)
			{
				query = query.Where(p => p.ProductCategoryId == categoryId.Value);
			}
			if (maxPrice.HasValue)
			{
				query = query.Where(p => p.Price <= maxPrice.Value);
			}
			products = await query.ToListAsync();
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		return Ok(products.Select(AdvertReadDto.FromEntity));
	}

	/// <summary>
	/// Get all services adverts
	/// q query parameter allows to filter services by title or description containing the provided keyword (case-insensitive)
	/// 
	/// GET: AdvertsController/GetServices
	/// Url: /api/v1/adverts/services
	/// </summary>
	/// <param name="q">The keyword to search for</param>
	/// <returns>List of formatted service adverts</returns>
	[HttpGet("services")]
	public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetServices([FromQuery] string? q = null)
	{
		IEnumerable<TutoringAdvert> services;
		try
		{
			var query = _context.Services
				.Include(s => s.Seller)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(q))
			{
				var probe = q.Trim().ToLower();
				query = query.Where(s =>
					s.Title.ToLower().Contains(probe)
					|| s.Description.ToLower().Contains(probe));
			}

			services = await query.ToListAsync();
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}

		return Ok(services.Select(AdvertReadDto.FromEntity));
	}

	/// <summary>
	/// Get the details of a specific PhysicalItem by its ID, 
	/// regardless of its type (Books, product or service).
	/// 
	/// GET: AdvertsController/Details/5
	/// Url: /api/v1/adverts/5
	/// </summary>
	/// <param name="id">The ID of the PhysicalItem to retrieve</param>
	/// <returns>The formatted PhysicalItem details</returns>
	[HttpGet("{id}")]
	public async Task<ActionResult<AdvertReadDto>> Details(long id)
	{
		Advert? adverts;
		try
		{
			adverts = await _context.Adverts
				.Include(a => a.Seller)
				.FirstOrDefaultAsync(a => a.AdvertId == id);
			if (adverts is PhysicalItem physicalItems)
			{
				await _context.Entry(physicalItems)
					.Collection(item => item.Pictures)
					.LoadAsync();
			}
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (adverts == null) return NotFound();

		return Ok(AdvertReadDto.FromEntity(adverts));
	}

	/// <summary>
	/// Get the details of a specific Books by its ID.
	/// 
	/// GET: AdvertsController/GetBookById/5
	/// Url: /api/v1/adverts/books/5
	/// </summary>
	/// <param name="id">The ID of the Books to retrieve</param>
	/// <returns>The formatted Books details</returns>
	[HttpGet("books/{id}")]
	public async Task<ActionResult<BookReadDto>> GetBookById(long id)
	{
		Book? books;
		try
		{
			books = await _context.Books
				.Include(b => b.Seller)
				.Include(b => b.Pictures)
				.Include(b => b.BookCategory)
				.FirstOrDefaultAsync(b => b.AdvertId == id);
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (books == null) return NotFound();
		return Ok(BookReadDto.FromEntity(books));
	}

	/// <summary>
	/// Get the details of a specific product by its ID.
	/// 
	/// GET: AdvertsController/GetProductById/5
	/// Url: /api/v1/adverts/products/5
	/// </summary>
	/// <param name="id">The ID of the product to retrieve</param>
	/// <returns>The formatted product details</returns>
	[HttpGet("products/{id}")]
	public async Task<ActionResult<ProductReadDto>> GetProductById(long id)
	{
		PhysicalItem? product;
		try
		{
			product = await _context.Products
				.Include(p => p.Seller)
				.Include(p => p.Pictures)
				.Where(p => !_context.Set<Book>().Any(b => b.AdvertId == p.AdvertId))
				.FirstOrDefaultAsync(p => p.AdvertId == id);
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (product == null) return NotFound();
		return Ok(ProductReadDto.FromEntity(product));
	}

	/// <summary>
	/// Get the details of a specific service by its ID.
	/// 
	/// GET: AdvertsController/GetServiceById/5
	/// Url: /api/v1/adverts/services/5
	/// </summary>
	/// <param name="id">The ID of the service to retrieve</param>
	/// <returns>The formatted service details</returns>
	[HttpGet("services/{id}")]
	public async Task<ActionResult<ServiceReadDto>> GetServiceById(long id)
	{
		TutoringAdvert? service;
		try
		{
			service = await _context.Services
				.Include(s => s.Seller)
				.Include(s => s.Subject)
				.Include(s => s.SchoolGrade)
				.FirstOrDefaultAsync(s => s.AdvertId == id);
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (service == null) return NotFound();
		return Ok(ServiceReadDto.FromEntity(service));
	}

	/// <summary>
	/// Mock catalogue summaries (T5-1: Books-only filters <c>isbn</c> / <c>q</c>). GET api/v1/adverts/summary
	/// </summary>
	[HttpGet("summary")]
	public async Task<IActionResult> GetSummaries(
		[FromQuery] AdvertSearchQuery? query,
		CancellationToken cancellationToken = default)
	{
		var items = await _advertSearchService.SearchSummariesAsync(query, cancellationToken);
		return Ok(items);
	}

	/// <summary>
	/// Mock catalogue detail by GUID (same dataset as /summary). GET api/v1/adverts/summary/{id}
	/// </summary>
	[HttpGet("summary/{id:guid}")]
	public async Task<IActionResult> GetSummaryDetail(Guid id, CancellationToken cancellationToken = default)
	{
		var detail = await _advertSearchService.GetDetailAsync(id, cancellationToken);
		if (detail is null)
		{
			return NotFound();
		}

		return Ok(detail);
	}
	#endregion

	#region POST METHODS

	/// <summary>
	/// Create a new Books PhysicalItem with the provided details in the request body.
	/// The PhysicalItem will be added to the database and the created PhysicalItem details will be returned in the response.
	/// 
	/// POST: AdvertsController/CreateBook
	/// Url: /api/v1/adverts/books
	/// </summary>
	/// <param name="bookDto">The DTO containing the Books PhysicalItem details</param>
	/// <returns>The created PhysicalItem details</returns>
	[HttpPost("books")]
	public async Task<ActionResult<AdvertReadDto>> CreateBook([FromBody] BookCreateDto bookDto)
	{
		if (bookDto == null) return BadRequest();

		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		Book book = bookDto.ToEntity();

		_context.Books.Add(book);
		await _context.SaveChangesAsync();

		AdvertReadDto readDto = AdvertReadDto.FromEntity(book);
		return CreatedAtAction("GetBooks", new { id = book.AdvertId }, readDto);
	}

	/// <summary>
	/// Create a new product PhysicalItem with the provided details in the request body.
	/// 
	/// POST: AdvertsController/CreateProduct
	/// Url: /api/v1/adverts/products
	/// </summary>
	/// <param name="productDto">The DTO containing the product PhysicalItem details</param>
	/// <returns>The created PhysicalItem details</returns>
	[HttpPost("products")]
	public async Task<ActionResult<AdvertReadDto>> CreateProduct([FromBody] ProductCreateDto productDto)
	{
		if (productDto == null) return BadRequest();

		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		PhysicalItem product = productDto.ToEntity();

		_context.Products.Add(product);
		await _context.SaveChangesAsync();

		AdvertReadDto readDto = AdvertReadDto.FromEntity(product);
		return CreatedAtAction("GetProducts", new { id = product.AdvertId }, readDto);
	}

	/// <summary>
	/// Create a new service PhysicalItem with the provided details in the request body.
	/// 
	/// POST: AdvertsController/CreateService
	/// Url: /api/v1/adverts/services
	/// </summary>
	/// <param name="serviceDto">The DTO containing the service PhysicalItem details</param>
	/// <returns>The created PhysicalItem details</returns>
	[HttpPost("services")]
	public async Task<ActionResult<AdvertReadDto>> CreateService([FromBody] ServiceCreateDto serviceDto)
	{
		if (serviceDto == null) return BadRequest();

		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		TutoringAdvert service = serviceDto.ToEntity();

		_context.Services.Add(service);
		await _context.SaveChangesAsync();

		AdvertReadDto readDto = AdvertReadDto.FromEntity(service);
		return CreatedAtAction("GetServices", new { id = service.AdvertId }, readDto);
	}
	#endregion

	#region PUT METHODS

	/// <summary>
	/// Edit an existing Books PhysicalItem with the provided details in the request body.
	/// 
	/// PUT: AdvertsController/EditBook/5
	/// Url: /api/v1/adverts/books/{id}
	/// </summary>
	/// <param name="id">The ID of the Books PhysicalItem to edit</param>
	/// <param name="bookDto">The DTO containing the updated Books PhysicalItem details</param>
	/// <returns>The updated PhysicalItem details</returns>
	[HttpPut("books/{id}")]
	public async Task<IActionResult> EditBook(long id, [FromBody] BookCreateDto bookDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		Book? existingBook = await _context.Books
			.Include(b => b.Pictures)
			.FirstOrDefaultAsync(b => b.AdvertId == id);

		if (existingBook == null) return NotFound();
		bookDto.MapToEntity(existingBook);

		try
		{
			_context.Entry(existingBook).State = EntityState.Modified;
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException e)
		{
			if (!AdvertExists(id)) return NotFound();
			return BadRequest(new { error = e.Message });
		}

		return NoContent();
	}

	/// <summary>
	/// Edit an existing product PhysicalItem with the provided details in the request body.
	/// 
	/// PUT: AdvertsController/EditProduct/5
	/// Url: /api/v1/adverts/products/{id}
	/// </summary>
	/// <param name="id">The ID of the product PhysicalItem to edit</param>
	/// <param name="productDto">The DTO containing the updated product PhysicalItem details</param>
	/// <returns>The updated PhysicalItem details</returns>
	[HttpPut("products/{id}")]
	public async Task<IActionResult> EditProduct(long id, [FromBody] ProductCreateDto productDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		PhysicalItem? existingProduct = await _context.Products
			.Include(p => p.Pictures)
			.FirstOrDefaultAsync(p => p.AdvertId == id);

		if (existingProduct == null) return NotFound();
		productDto.MapToEntity(existingProduct);

		try
		{
			_context.Entry(existingProduct).State = EntityState.Modified;
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException e)
		{
			if (!AdvertExists(id)) return NotFound();
			return BadRequest(new { error = e.Message });
		}

		return NoContent();
	}

	/// <summary>
	/// Edit an existing service PhysicalItem with the provided details in the request body.
	/// 
	/// PUT: AdvertsController/EditService/5
	/// Url: /api/v1/adverts/services/{id}
	/// </summary>
	/// <param name="id">The ID of the service PhysicalItem to edit</param>
	/// <param name="serviceDto">The DTO containing the updated service PhysicalItem details</param>
	/// <returns>The updated PhysicalItem details</returns>
	[HttpPut("services/{id}")]
	public async Task<IActionResult> EditService(long id, [FromBody] ServiceCreateDto serviceDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		TutoringAdvert? existingService = await _context.Services
			.FirstOrDefaultAsync(s => s.AdvertId == id);

		if (existingService == null) return NotFound();
		serviceDto.MapToEntity(existingService);

		try
		{
			_context.Entry(existingService).State = EntityState.Modified;
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException e)
		{
			if (!AdvertExists(id)) return NotFound();
			return BadRequest(new { error = e.Message });
		}

		return NoContent();
	}
	#endregion

	#region PATCH METHODS

	/// <summary>
	/// Update the status of an existing PhysicalItem.
	/// 
	/// PATCH: AdvertsController/UpdateAdvertStatus/5
	/// Url: /api/v1/adverts/{id}/status
	/// </summary>
	/// <param name="id">The ID of the PhysicalItem to update</param>
	/// <param name="status">The new status for the PhysicalItem</param>
	/// <returns>The updated PhysicalItem details</returns>
	[HttpPatch("{id}/status")]
	public async Task<IActionResult> UpdateAdvertStatus(long id, [FromBody] AdvertStatus status)
	{
		Advert? Adverts;
		try
		{
			Adverts = await _context.Adverts.FindAsync(id);
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (Adverts == null) return NotFound();

		Adverts.Status = status;
		_context.Entry(Adverts).State = EntityState.Modified;
		await _context.SaveChangesAsync();
		return NoContent();
	}
	#endregion

	#region DELETE METHODS

	/// <summary>
	/// Delete an existing PhysicalItem.
	/// 
	/// DELETE: AdvertsController/Delete/5
	/// Url: /api/v1/adverts/{id}
	/// </summary>
	/// <param name="id">The ID of the PhysicalItem to delete</param>
	/// <returns>The deleted PhysicalItem details</returns>
	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteAdvert(long id)
	{
		Advert? Adverts;
		try
		{
			Adverts = await _context.Adverts.FindAsync(id);
		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (Adverts == null) return NotFound();

		_context.Adverts.Remove(Adverts);
		await _context.SaveChangesAsync();
		return NoContent();
	}

	/// <summary>
	/// Remove images from an existing PhysicalItem.
	/// 
	/// DELETE: AdvertsController/RemoveImages
	/// Url: /api/v1/adverts/{id}/images
	/// </summary>
	/// <param name="id">The ID of the PhysicalItem from which to remove images</param>
	/// <param name="imageUrls">The list of image URLs to remove</param>
	/// <returns>The updated PhysicalItem details</returns>
	[HttpDelete("{id}/images")]
	public async Task<IActionResult> RemoveAdvertImages(long id, [FromBody] List<string> imageUrls)
	{
		PhysicalItem? product;
		try
		{
			product = await _context.Products
			.Include(p => p.Pictures)
			.FirstOrDefaultAsync(p => p.AdvertId == id);

		}
		catch (Exception e)
		{
			return BadRequest(new { error = e.Message });
		}
		if (product == null) return NotFound();

		ICollection<Picture> picturesToRemove = product.Pictures
			.Where(p => imageUrls.Contains(p.Label))
			.ToList();

		if (picturesToRemove.Any())
		{
			_context.Pictures.RemoveRange(picturesToRemove);
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (Exception e)
			{
				return BadRequest(new { error = e.Message });
			}
			return NoContent();
		}
		return BadRequest("No matching images found to remove.");
	}
	#endregion
}
