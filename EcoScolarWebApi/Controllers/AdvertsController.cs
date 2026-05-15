using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs.Advert;
using EcoscolarWebApi.Utils.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Billing;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdvertsController : Controller
    {
        private readonly EcoscolarDbContext _context;
        private bool AdvertExists(long id) => _context.Adverts.Any(e => e.AdvertId == id);

        public AdvertsController(EcoscolarDbContext context)
        {
            _context = context;
        }

        // GET METHODS

        // GET: AdvertsController
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

        // GET: AdvertsController/GetBooks
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

        // GET: AdvertsController/GetProducts
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

        // GET: AdvertsController/GetServices
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

        // GET: AdvertsController/Details/5
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

        // POST METHODS

        // POST: AdvertsController/CreateBook
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

        // POST: AdvertsController/CreateProduct
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

        // POST: AdvertsController/CreateService
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

        // PUT: AdvertsController/EditBook/5
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

        // PUT: AdvertsController/EditProduct/5
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

        // PUT: AdvertsController/EditService/5
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

        // PATCH: AdvertsController/UpdateAdvertStatus/5
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

        // DELETE: AdvertsController/Delete/5
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

        // DELETE: AdvertsController/RemoveImages
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
