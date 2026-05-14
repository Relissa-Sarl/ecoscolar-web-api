using EcoscolarWebApi.Data;
using EcoscolarWebApi.Models;
using EcoscolarWebApi.Utils.enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdvertsController : Controller
    {
        private readonly EcoscolarDbContext _context;

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
        public async Task<ActionResult<IEnumerable<Books>>> GetBooks()
        {
            return await _context.Books.ToListAsync();
        }

        // GET: AdvertsController/GetProducts
        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<PhysicalItems>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // GET: AdvertsController/GetServices
        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<AdvertServices>>> GetServices()
        {
            return await _context.Services.ToListAsync();
        }

        // GET: AdvertsController/Details/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Adverts>> Details(int id)
        {
            var advert = await _context.Adverts.FindAsync(id);
            if (advert == null)
            {
                return NotFound();
            }
            return advert;
        }

        // POST METHODS

        // POST: AdvertsController/CreateBook
        [HttpPost("books")]
        public async Task<ActionResult<Books>> CreateBook([FromBody] Books book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetBooks", new { id = book.AdvertId }, book);
        }

        // POST: AdvertsController/CreateProduct
        [HttpPost("products")]
        public async Task<ActionResult<PhysicalItems>> CreateProduct([FromBody] PhysicalItems product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetProducts", new { id = product.AdvertId }, product);
        }

        // POST: AdvertsController/CreateService
        [HttpPost("services")]
        public async Task<ActionResult<AdvertServices>> CreateService([FromBody] AdvertServices service)
        {
            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetServices", new { id = service.AdvertId }, service);
        }

        // PUT METHODS

        // PUT: AdvertsController/EditBook/5
        [HttpPut("books/{id}")]
        public async Task<IActionResult> EditBook(int id, [FromBody] Books book)
        {
            if (id != book.AdvertId)
            {
                return BadRequest();
            }
            _context.Entry(book).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: AdvertsController/EditProduct/5
        [HttpPut("products/{id}")]
        public async Task<IActionResult> EditProduct(int id, [FromBody] PhysicalItems product)
        {
            if (id != product.AdvertId)
            {
                return BadRequest();
            }
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: AdvertsController/EditService/5
        [HttpPut("services/{id}")]
        public async Task<IActionResult> EditService(int id, [FromBody] AdvertServices service)
        {
            if (id != service.AdvertId)
            {
                return BadRequest();
            }
            _context.Entry(service).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH METHODS

        // PATCH: AdvertsController/UpdateAdvertStatus/5
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateAdvertStatus(int id, [FromBody] AdvertStatus status)
        {
            Adverts? advert = await _context.Adverts.FindAsync(id);
            if (advert == null)
            {
                return NotFound();
            }
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
            if (advert == null)
            {
                return NotFound();
            }
            _context.Adverts.Remove(advert);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: AdvertsController/RemoveImages
        [HttpDelete("{id}/images")]
        public async Task<IActionResult> RemoveAdvertImages(int id, [FromBody] List<string> imageUrls)
        {
            PhysicalItems? advert = await _context.Products.FindAsync(id);
            if (advert == null)
            {
                return NotFound();
            }
            advert.Images = advert.Images.Where(url => !imageUrls.Contains(url)).ToList();
            _context.Entry(advert).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
