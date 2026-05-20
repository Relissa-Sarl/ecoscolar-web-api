using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using Asp.Versioning;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class SchoolGradesController : ControllerBase
{
    private readonly EcoscolarDbContext _context;
    public SchoolGradesController(EcoscolarDbContext context)
    {
        _context = context;
    }

    // GET: api/SchoolGrades
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolGrade>>> GetSchoolGrades()
    {
        return await _context.SchoolGrades.ToListAsync();
    }

    // GET: api/SchoolGrades/5
    [HttpGet("{schoolgradeid}")]
    public async Task<ActionResult<SchoolGrade>> GetSchoolGrades(long schoolgradeid)
    {
        var schoolgrades = await _context.SchoolGrades.FindAsync(schoolgradeid);

        if (schoolgrades == null)
        {
            return NotFound();
        }

        return schoolgrades;
    }

    // PUT: api/SchoolGrades/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{schoolgradeid}")]
    public async Task<IActionResult> PutSchoolGrades(long schoolgradeid, SchoolGradeCreateUpdateDto dto)
    {
        var existingGrade = await _context.SchoolGrades.FindAsync(schoolgradeid);

        if (existingGrade == null)
        {
            return NotFound();
        }

        existingGrade.Name = dto.Name;
        existingGrade.Code = dto.SchoolGrade;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SchoolGradesExists(schoolgradeid))
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

    // POST: api/SchoolGrades
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<SchoolGrade>> PostSchoolGrades(SchoolGradeCreateUpdateDto dto)
    {
        var schoolgrade = new SchoolGrade
        {
            Name = dto.Name,
            Code = dto.SchoolGrade
        };

        _context.SchoolGrades.Add(schoolgrade);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSchoolGrades", new { schoolgradeid = schoolgrade.SchoolGradeId }, schoolgrade);
    }

    // DELETE: api/SchoolGrades/5
    [HttpDelete("{schoolgradeid}")]
    public async Task<IActionResult> DeleteSchoolGrades(long? schoolgradeid)
    {
        var schoolgrades = await _context.SchoolGrades.FindAsync(schoolgradeid);
        if (schoolgrades == null)
        {
            return NotFound();
        }

        _context.SchoolGrades.Remove(schoolgrades);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SchoolGradesExists(long? schoolgradeid)
    {
        return _context.SchoolGrades.Any(e => e.SchoolGradeId == schoolgradeid);
    }
}
