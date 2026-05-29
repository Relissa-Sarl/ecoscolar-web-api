using Asp.Versioning;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.ReferenceData;
using EcoScolarWebApi.Mappers;
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
    private readonly SubjectMapper _mapper; // Étape 1 : Injection du mapper

    public SubjectsController(EcoscolarDbContext context, SubjectMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/v1/Subjects
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectResponse>>> GetSubjects()
    {
        var subjects = await _context.Subjects.ToListAsync();

        // Utilisation de Mapperly pour la liste
        return Ok(_mapper.ToResponseList(subjects));
    }

    // GET: api/v1/Subjects/5
    [HttpGet("{subjectid}")]
    public async Task<ActionResult<SubjectResponse>> GetSubjects(long subjectid)
    {
        var subject = await _context.Subjects.FindAsync(subjectid);

        if (subject == null)
        {
            return NotFound();
        }

        // Utilisation de Mapperly pour un objet unique
        return Ok(_mapper.ToResponse(subject));
    }

    // PUT: api/v1/Subjects/5
    [HttpPut("{subjectid}")]
    public async Task<IActionResult> PutSubjects(long subjectid, SubjectRequest request)
    {
        var existingSubject = await _context.Subjects.FindAsync(subjectid);

        if (existingSubject == null)
        {
            return NotFound();
        }

        // Utilisation de Mapperly pour mettre à jour l'entité existante avec les données de la request
        _mapper.UpdateEntity(request, existingSubject);

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

    // POST: api/v1/Subjects
    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> PostSubjects(SubjectRequest request)
    {
        // Utilisation de Mapperly pour transformer la request en entité
        var subject = _mapper.ToEntity(request);

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        // On renvoie le SubjectResponse généré à partir de l'entité tout juste créée
        var response = _mapper.ToResponse(subject);

        return CreatedAtAction(nameof(GetSubjects), new { subjectid = subject.SubjectId }, response);
    }

    // DELETE: api/v1/Subjects/5
    [HttpDelete("{subjectid}")]
    public async Task<IActionResult> DeleteSubjects(long subjectid)
    {
        var subject = await _context.Subjects.FindAsync(subjectid);
        if (subject == null)
        {
            return NotFound();
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SubjectsExists(long subjectid)
    {
        return _context.Subjects.Any(e => e.SubjectId == subjectid);
    }
}