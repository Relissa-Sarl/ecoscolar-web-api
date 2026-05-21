using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoScolarWebApi.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class SubjectsController : ControllerBase
{
    private readonly EcoscolarDbContext _context;
    public SubjectsController(EcoscolarDbContext context)
    {
        _context = context;
    }

    // GET: api/Subjects
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subject>>> GetSubjects()
    {
        return await _context.Subjects.ToListAsync();
    }

    // GET: api/Subjects/5
    [HttpGet("{subjectid}")]
    public async Task<ActionResult<Subject>> GetSubjects(long subjectid)
    {
        var subjects = await _context.Subjects.FindAsync(subjectid);

        if (subjects == null)
        {
            return NotFound();
        }

        return subjects;
    }

    // PUT: api/Subjects/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{subjectid}")]
    public async Task<IActionResult> PutSubjects(long subjectid, SubjectCreateUpdateDto dto)
    {
        var existingSubject = await _context.Subjects.FindAsync(subjectid);

        if (existingSubject == null)
        {
            return NotFound();
        }

        existingSubject.Name = dto.Name;
        existingSubject.Code = dto.Subject;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SubjectsExists(subjectid))
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

    // POST: api/Subjects
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Subject>> PostSubjects(SubjectCreateUpdateDto dto)
    {
        var subject = new Subject
        {
            Name = dto.Name,
            Code = dto.Subject
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSubjects", new { subjectid = subject.SubjectId }, subject);
    }

    // DELETE: api/Subjects/5
    [HttpDelete("{subjectid}")]
    public async Task<IActionResult> DeleteSubjects(long? subjectid)
    {
        var subjects = await _context.Subjects.FindAsync(subjectid);
        if (subjects == null)
        {
            return NotFound();
        }

        _context.Subjects.Remove(subjects);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SubjectsExists(long? subjectid)
    {
        return _context.Subjects.Any(e => e.SubjectId == subjectid);
    }
}
