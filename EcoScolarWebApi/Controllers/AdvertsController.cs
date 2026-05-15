using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs.Advert;
using EcoscolarWebApi.Utils.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdvertsController : Controller
    {
        private readonly EcoscolarDbContext _context;                                           // Database context for accessing the database
        private bool AdvertExists(long id) => _context.Adverts.Any(e => e.AdvertId == id);      // Helper method to check if an advert with the specified ID exists in the database

        /// <summary>
        /// AdvertsController constructor
        /// </summary>
        /// <param name="context">The database context</param>
        public AdvertsController(EcoscolarDbContext context)
        {
            _context = context;
        }

        // GET METHODS

        /// <summary>
        /// Get all adverts whatever their type is (book, product or service)
        /// 
        /// GET: AdvertsController
        /// Url: /api/v1/adverts
        /// </summary>
        /// <returns>List of all formatted adverts</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> Index()
        {
            IEnumerable<Adverts> adverts;
            try
            {
                adverts = await _context.Adverts.ToListAsync();

            }
            catch (Exception e)
            {
                throw;
            }
            return Ok(adverts.Select(AdvertReadDto.FromEntity));
        }

        /// <summary>
        /// Get all books adverts
        /// 
        /// GET: AdvertsController/GetBooks
        /// Url: /api/v1/adverts/books
        /// </summary>
        /// <returns>List of formatted book adverts</returns>
        [HttpGet("books")]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetBooks()
        {
            IEnumerable<Books> books;
            try
            {
                books = await _context.Books.Include(p => p.Pictures).ToListAsync();

            }
            catch (Exception e)
            {
                throw;
            }
            return Ok(books.Select(AdvertReadDto.FromEntity));
        }

        /// <summary>
        /// Get all products adverts
        /// 
        /// GET: AdvertsController/GetProducts
        /// Url: /api/v1/adverts/products
        /// </summary>
        /// <returns>List of formatted product adverts</returns>
        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetProducts()
        {
            IEnumerable<PhysicalItems> products;
            try
            {
                products = await _context.Products.Include(p => p.Pictures).ToListAsync();
            }
            catch (Exception e)
            {
                throw;
            }
            return Ok(products.Select(AdvertReadDto.FromEntity));
        }

        /// <summary>
        /// Get all services adverts
        /// 
        /// GET: AdvertsController/GetServices
        /// Url: /api/v1/adverts/services
        /// </summary>
        /// <returns>List of formatted service adverts</returns>
        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetServices()
        {
            IEnumerable<AdvertServices> services;
            try
            {
                services = await _context.Services.ToListAsync();
            }
            catch (Exception e)
            {
                throw;
            }
            return Ok(services.Select(AdvertReadDto.FromEntity));
        }

        /// <summary>
        /// Get the details of a specific advert by its ID, 
        /// regardless of its type (book, product or service).
        /// 
        /// GET: AdvertsController/Details/5
        /// Url: /api/v1/adverts/5
        /// </summary>
        /// <param name="id">The ID of the advert to retrieve</param>
        /// <returns>The formatted advert details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<AdvertReadDto>> Details(long id)
        {
            Adverts advert;
            try
            {
                advert = await _context.Adverts.FindAsync(id);
            } catch (Exception e)
            {
                throw;
            }
            if (advert == null) return NotFound();
            
            return Ok(AdvertReadDto.FromEntity(advert));
        }
        
        [HttpGet]
        public async Task<IActionResult> GetSummaries(CancellationToken cancellationToken = default)
        {
            var items = await _advertSearchService.GetSummariesAsync();
            return Ok(items);
        }

        // POST METHODS

        /// <summary>
        /// Create a new book advert with the provided details in the request body.
        /// The advert will be added to the database and the created advert details will be returned in the response.
        /// 
        /// POST: AdvertsController/CreateBook
        /// Url: /api/v1/adverts/books
        /// </summary>
        /// <param name="bookDto">The DTO containing the book advert details</param>
        /// <returns>The created advert details</returns>
        [HttpPost("books")]
        public async Task<ActionResult<AdvertReadDto>> CreateBook([FromBody] BookCreateDto bookDto)
        {
            if (bookDto == null) return NotFound();

            Books book = bookDto.ToEntity();

            _context.Books.Add(book);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw;
            }

            AdvertReadDto readDto = AdvertReadDto.FromEntity(book);
            return CreatedAtAction("GetBooks", new { id = book.AdvertId }, readDto);
        }

        /// <summary>
        /// Create a new product advert with the provided details in the request body.
        /// 
        /// POST: AdvertsController/CreateProduct
        /// Url: /api/v1/adverts/products
        /// </summary>
        /// <param name="productDto">The DTO containing the product advert details</param>
        /// <returns>The created advert details</returns>
        [HttpPost("products")]
        public async Task<ActionResult<AdvertReadDto>> CreateProduct([FromBody] ProductCreateDto productDto)
        {
            if (productDto == null) return NotFound();

            PhysicalItems product = productDto.ToEntity();

            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw;
            }

            AdvertReadDto readDto = AdvertReadDto.FromEntity(product);
            return CreatedAtAction("GetProducts", new { id = product.AdvertId }, readDto);
        }

        /// <summary>
        /// Create a new service advert with the provided details in the request body.
        /// 
        /// POST: AdvertsController/CreateService
        /// Url: /api/v1/adverts/services
        /// </summary>
        /// <param name="serviceDto">The DTO containing the service advert details</param>
        /// <returns>The created advert details</returns>
        [HttpPost("services")]
        public async Task<ActionResult<AdvertReadDto>> CreateService([FromBody] ServiceCreateDto serviceDto)
        {
            if (serviceDto == null) return NotFound();

            AdvertServices service = serviceDto.ToEntity();

            _context.Services.Add(service);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw;
            }

            AdvertReadDto readDto = AdvertReadDto.FromEntity(service);
            return CreatedAtAction("GetServices", new { id = service.AdvertId }, readDto);
        }

        // PUT METHODS

        /// <summary>
        /// Edit an existing book advert with the provided details in the request body.
        /// 
        /// PUT: AdvertsController/EditBook/5
        /// Url: /api/v1/adverts/books/{id}
        /// </summary>
        /// <param name="id">The ID of the book advert to edit</param>
        /// <param name="bookDto">The DTO containing the updated book advert details</param>
        /// <returns>The updated advert details</returns>
        [HttpPut("books/{id}")]
        public async Task<IActionResult> EditBook(long id, [FromBody] BookCreateDto bookDto)
        {
            Books existingBook = await _context.Books
                .Include(b => b.Pictures)
                .FirstOrDefaultAsync(b => b.AdvertId == id);
            
            if(existingBook == null) return NotFound();
            bookDto.MapToEntity(existingBook);

            try
            {
                _context.Entry(existingBook).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AdvertExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Edit an existing product advert with the provided details in the request body.
        /// 
        /// PUT: AdvertsController/EditProduct/5
        /// Url: /api/v1/adverts/products/{id}
        /// </summary>
        /// <param name="id">The ID of the product advert to edit</param>
        /// <param name="productDto">The DTO containing the updated product advert details</param>
        /// <returns>The updated advert details</returns>
        [HttpPut("products/{id}")]
        public async Task<IActionResult> EditProduct(long id, [FromBody] ProductCreateDto productDto)
        {
            PhysicalItems existingProduct = await _context.Products
                .Include(p => p.Pictures)
                .FirstOrDefaultAsync(p => p.AdvertId == id);

            if (existingProduct == null) return NotFound();
            productDto.MapToEntity(existingProduct);

            try
            {
                _context.Entry(existingProduct).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AdvertExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Edit an existing service advert with the provided details in the request body.
        /// 
        /// PUT: AdvertsController/EditService/5
        /// Url: /api/v1/adverts/services/{id}
        /// </summary>
        /// <param name="id">The ID of the service advert to edit</param>
        /// <param name="serviceDto">The DTO containing the updated service advert details</param>
        /// <returns>The updated advert details</returns>
        [HttpPut("services/{id}")]
        public async Task<IActionResult> EditService(long id, [FromBody] ServiceCreateDto serviceDto)
        {
            AdvertServices existingService = await _context.Services
                .FirstOrDefaultAsync(s => s.AdvertId == id);

            if (existingService == null) return NotFound();
            serviceDto.MapToEntity(existingService);

            try
            {
                _context.Entry(existingService).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AdvertExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // PATCH METHODS

        /// <summary>
        /// Update the status of an existing advert.
        /// 
        /// PATCH: AdvertsController/UpdateAdvertStatus/5
        /// Url: /api/v1/adverts/{id}/status
        /// </summary>
        /// <param name="id">The ID of the advert to update</param>
        /// <param name="status">The new status for the advert</param>
        /// <returns>The updated advert details</returns>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateAdvertStatus(long id, [FromBody] AdvertStatus status)
        {
            Adverts? advert;
            try
            {
                advert = await _context.Adverts.FindAsync(id);
            }
            catch (Exception e)
            {
                throw;
            }
            if (advert == null) return NotFound();

            advert.Status = status;
            _context.Entry(advert).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE METHODS

        /// <summary>
        /// Delete an existing advert.
        /// 
        /// DELETE: AdvertsController/Delete/5
        /// Url: /api/v1/adverts/{id}
        /// </summary>
        /// <param name="id">The ID of the advert to delete</param>
        /// <returns>The deleted advert details</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdvert(long id)
        {
            Adverts? advert;
            try
            {
                advert = await _context.Adverts.FindAsync(id);
            } catch (Exception e) {
                throw;
            }
            if (advert == null) return NotFound();

            _context.Adverts.Remove(advert);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Remove images from an existing advert.
        /// 
        /// DELETE: AdvertsController/RemoveImages
        /// Url: /api/v1/adverts/{id}/images
        /// </summary>
        /// <param name="id">The ID of the advert from which to remove images</param>
        /// <param name="imageUrls">The list of image URLs to remove</param>
        /// <returns>The updated advert details</returns>
        [HttpDelete("{id}/images")]
        public async Task<IActionResult> RemoveAdvertImages(long id, [FromBody] List<string> imageUrls)
        {
            PhysicalItems? product;
            try
            {
                product = await _context.Products
                .Include(p => p.Pictures)
                .FirstOrDefaultAsync(p => p.AdvertId == id);

            }
            catch (Exception e)
            {
                throw;
            }
            if (product == null) return NotFound();

            ICollection<Pictures> picturesToRemove = product.Pictures
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
                    throw;
                }
                return NoContent();
            }
            return BadRequest("No matching images found to remove.");
        }
    }
}
