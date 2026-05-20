using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;

[Route("api/v1/[controller]")]
[ApiController]
public class ProductCategoriesController : ControllerBase
{
    private readonly EcoscolarDbContext _context;
    public ProductCategoriesController(EcoscolarDbContext context)
    {
        _context = context;
    }

    // GET: api/ProductCategories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductCategories>>> GetProductCategories()
    {
        return await _context.ProductCategories.ToListAsync();
    }

    // GET: api/ProductCategories/5
    [HttpGet("{productcategoryid}")]
    public async Task<ActionResult<ProductCategories>> GetProductCategories(long productcategoryid)
    {
        var productcategories = await _context.ProductCategories.FindAsync(productcategoryid);

        if (productcategories == null)
        {
            return NotFound();
        }

        return productcategories;
    }

    // PUT: api/ProductCategories/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{productcategoryid}")]
    public async Task<IActionResult> PutProductCategories(long productcategoryid, ProductCategoryCreateUpdateDto dto)
    {
        var existingCategory = await _context.ProductCategories.FindAsync(productcategoryid);

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
            if (!ProductCategoriesExists(productcategoryid))
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

    // POST: api/ProductCategories
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<ProductCategories>> PostProductCategories(ProductCategoryCreateUpdateDto dto)
    {
        var productcategory = new ProductCategories
        {
            Name = dto.Name,
            Description = dto.Description
        };

        _context.ProductCategories.Add(productcategory);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetProductCategories", new { productcategoryid = productcategory.ProductCategoryId }, productcategory);
    }

    // DELETE: api/ProductCategories/5
    [HttpDelete("{productcategoryid}")]
    public async Task<IActionResult> DeleteProductCategories(long? productcategoryid)
    {
        var productcategories = await _context.ProductCategories.FindAsync(productcategoryid);
        if (productcategories == null)
        {
            return NotFound();
        }

        _context.ProductCategories.Remove(productcategories);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductCategoriesExists(long? productcategoryid)
    {
        return _context.ProductCategories.Any(e => e.ProductCategoryId == productcategoryid);
    }
}
