using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;

[Route("api/v1/[controller]")]
[ApiController]
public class BookCategoriesController : ControllerBase
{
    private readonly EcoscolarDbContext _context;
    public BookCategoriesController(EcoscolarDbContext context)
    {
        _context = context;
    }

    // GET: api/BookCategories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookCategories>>> GetBookCategories()
    {
        return await _context.BookCategories.ToListAsync();
    }

    // GET: api/BookCategories/5
    [HttpGet("{bookcategoryid}")]
    public async Task<ActionResult<BookCategories>> GetBookCategories(long bookcategoryid)
    {
        var bookcategories = await _context.BookCategories.FindAsync(bookcategoryid);

        if (bookcategories == null)
        {
            return NotFound();
        }

        return bookcategories;
    }

    // PUT: api/BookCategories/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{bookcategoryid}")]
    public async Task<IActionResult> PutBookCategories(long bookcategoryid, BookCategoryCreateUpdateDto dto)
    {
        var existingCategory = await _context.BookCategories.FindAsync(bookcategoryid);

        if (existingCategory == null)
        {
            return NotFound();
        }

        existingCategory.Name = dto.Name;
        existingCategory.Description = dto.Description;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BookCategoriesExists(bookcategoryid))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/BookCategories
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<BookCategories>> PostBookCategories(BookCategoryCreateUpdateDto dto)
    {
        var bookcategory = new BookCategories
        {
            Name = dto.Name,
            Description = dto.Description
        };

        _context.BookCategories.Add(bookcategory);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetBookCategories", new { bookcategoryid = bookcategory.BookCategoryId }, bookcategory);
    }

    // DELETE: api/BookCategories/5
    [HttpDelete("{bookcategoryid}")]
    public async Task<IActionResult> DeleteBookCategories(long? bookcategoryid)
    {
        var bookcategories = await _context.BookCategories.FindAsync(bookcategoryid);
        if (bookcategories == null)
        {
            return NotFound();
        }

        _context.BookCategories.Remove(bookcategories);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool BookCategoriesExists(long? bookcategoryid)
    {
        return _context.BookCategories.Any(e => e.BookCategoryId == bookcategoryid);
    }
}
