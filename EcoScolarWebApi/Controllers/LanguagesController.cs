
using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Mappers;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class LanguagesController : Controller
{
    private readonly EcoscolarDbContext _context;
    private readonly LanguageMapper _mapper;

    public LanguagesController(EcoscolarDbContext context, LanguageMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/v1/Languages
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LanguageResponse>>> GetLanguages()    
    {
        var languages = await _context.Languages.ToListAsync();
        return Ok(_mapper.ToResponseList(languages));
    }

    // GET: api/v1/Languages/FR
    [HttpGet("{label}")]
    public async Task<ActionResult<LanguageResponse>> GetLanguages(string label)
    {
        var language = await _context.Languages.FindAsync(label);

        if (language == null)
        {
            return NotFound();
        }

        return Ok(_mapper.ToResponse(language));
    }

    // PUT: api/v1/Languages/FR
    [HttpPut("{label}")]
    public async Task<IActionResult> PutLanguages(string label, LanguageRequest request)
    {
        var existingLanguage = await _context.Languages.FindAsync(label);
        if (existingLanguage == null)
        {
            return NotFound();
        }

        _mapper.UpdateEntity(request, existingLanguage);

        try
        {
            await _context.SaveChangesAsync();

        }
        catch (DbUpdateConcurrencyException)
        {
            if (!LanguageExists(label))
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

    //POST: api/v1/Languages
    [HttpPost]
    public async Task<ActionResult<LanguageResponse>> PostLanguages(LanguageRequest request)
    {
        var language = _mapper.ToEntity(request);

        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        var response = _mapper.ToResponse(language);

        return CreatedAtAction(nameof(GetLanguages), new { label = language.Label }, response);
    }

    // DELETE: api/v1/Languages/FR
    [HttpDelete("{label}")]
    public async Task<IActionResult> DeleteLanguages(string label)
    {
        var language = await _context.Languages.FindAsync(label);
        if (language == null)
        {
            return NotFound();
        }

        _context.Languages.Remove(language);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool LanguageExists(string label)
    {
        return _context.Languages.Any(e => e.Label == label);
    }
}
