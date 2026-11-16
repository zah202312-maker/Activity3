using ActivityApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>القوائم المتسلسلة (يقابل findOffic / findSection / findSubsid في Java)</summary>
[Route("Lookup")]
public class LookupController : Controller
{
    private readonly AppDbContext _db;
    public LookupController(AppDbContext db) => _db = db;

    [HttpGet("Offices")]
    public async Task<IActionResult> Offices(int governorateId) =>
        Json(await _db.Offices.Where(o => o.GovernorateId == governorateId).OrderBy(o => o.Name)
            .Select(o => new { id = o.Id, name = o.Name }).ToListAsync());

    [HttpGet("Sections")]
    public async Task<IActionResult> Sections(int officeId) =>
        Json(await _db.Sections.Where(s => s.OfficeId == officeId).OrderBy(s => s.Name)
            .Select(s => new { id = s.Id, name = s.Name }).ToListAsync());

    [HttpGet("Subsids")]
    public async Task<IActionResult> Subsids(int sectionId) =>
        Json(await _db.Subsids.Where(s => s.SectionId == sectionId).OrderBy(s => s.Name)
            .Select(s => new { id = s.Id, name = s.Name }).ToListAsync());
}
