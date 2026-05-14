using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.DTOs.Advert;
using EcoscolarWebApi.Utils.Enums;
using Microsoft.AspNetCore.Http;
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
        private bool AdvertExists(int id) => _context.Adverts.Any(e => e.AdvertId == id);

        public AdvertsController(EcoscolarDbContext context)
        {
            _context = context;
        }

        // GET METHODS

        // GET: AdvertsController
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Adverts>>> Index()
        {
            return await _context.Adverts.ToListAsync();
        }

        // GET: AdvertsController/GetBooks
        [HttpGet("books")]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetBooks()
        {
            var books = await _context.Books.Include(p => p.Pictures).ToListAsync();
            return Ok(books.Select(AdvertReadDto.FromEntity));
        }

        // GET: AdvertsController/GetProducts
        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetProducts()
        {
            var products = await _context.Products.Include(p => p.Pictures).ToListAsync();
            return Ok(products.Select(AdvertReadDto.FromEntity));
        }

        // GET: AdvertsController/GetServices
        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<AdvertReadDto>>> GetServices()
        {
            var services = await _context.Services.ToListAsync();
            return Ok(services.Select(AdvertReadDto.FromEntity));
        }

        // GET: AdvertsController/Details/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Adverts>> Details(int id)
        {
            var advert = await _context.Adverts.FindAsync(id);
            if (advert == null) return NotFound();
            
            return advert;
        }

        // POST METHODS

        // POST: AdvertsController/CreateBook
        [HttpPost("books")]
        public async Task<ActionResult<AdvertReadDto>> CreateBook([FromBody] BookCreateDto bookDto)
        {
            if (bookDto == null) return NotFound();

            Books book = bookDto.ToEntity();

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

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
            await _context.SaveChangesAsync();

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
            await _context.SaveChangesAsync();

            AdvertReadDto readDto = AdvertReadDto.FromEntity(service);
            return CreatedAtAction("GetServices", new { id = service.AdvertId }, readDto);
        }

        // PUT METHODS

        // PUT: AdvertsController/EditBook/5
        [HttpPut("books/{id}")]
        public async Task<IActionResult> EditBook(int id, [FromBody] BookCreateDto bookDto)
        {
            Books existingBook = await _context.Books
                .Include(b => b.Pictures)
                .FirstAsync(b => b.AdvertId == id);
            
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
        public async Task<IActionResult> EditProduct(int id, [FromBody] ProductCreateDto productDto)
        {
            PhysicalItems existingProduct = await _context.Products
                .Include(p => p.Pictures)
                .FirstAsync(p => p.AdvertId == id);

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
        public async Task<IActionResult> EditService(int id, [FromBody] ServiceCreateDto serviceDto)
        {
            AdvertServices existingService = await _context.Services
                .FirstAsync(s => s.AdvertId == id);

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
        public async Task<IActionResult> UpdateAdvertStatus(int id, [FromBody] AdvertStatus status)
        {
            Adverts? advert = await _context.Adverts.FindAsync(id);
            if (advert == null) return NotFound();

            advert.Status = status;
            _context.Entry(advert).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE METHODS

        // DELETE: AdvertsController/Delete/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdvert(int id)
        {
            Adverts? advert = await _context.Adverts.FindAsync(id);
            if (advert == null) return NotFound();

            _context.Adverts.Remove(advert);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: AdvertsController/RemoveImages
        [HttpDelete("{id}/images")]
        public async Task<IActionResult> RemoveAdvertImages(int id, [FromBody] List<string> imageUrls)
        {
            PhysicalItems? product = await _context.Products
                .Include(p => p.Pictures)
                .FirstOrDefaultAsync(p => p.AdvertId == id);
            if (product == null) return NotFound();

            ICollection<Pictures> picturesToRemove = product.Pictures
                .Where(p => imageUrls.Contains(p.Label))
                .ToList();

            if (picturesToRemove.Any())
            {
                _context.Pictures.RemoveRange(picturesToRemove);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            return BadRequest("No matching images found to remove.");
        }
    }
}
