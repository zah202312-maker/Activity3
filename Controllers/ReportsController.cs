using ActivityApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل ReportsNewController + testcontroller.printbetween (JasperReports) — هنا صفحات قابلة للطباعة</summary>
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.People = new SelectList(await _db.People.OrderBy(p => p.Name).ToListAsync(), "Id", "Name");
        ViewBag.Activities = new SelectList(
            (await _db.Activities.OrderByDescending(a => a.Date).Take(300).ToListAsync())
            .Select(a => new { a.Id, Text = $"#{a.Id} - {a.Date:yyyy-MM-dd} - {a.ActivityPlace}" }), "Id", "Text");
        ViewBag.Offices = new SelectList(await _db.Offices.OrderBy(o => o.Name).ToListAsync(), "Id", "Name");
        ViewBag.OfficePeople = await _db.People.Where(p => p.OfficeId != null).OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.OfficeId }).ToListAsync();
        return View();
    }

    /// <summary>تقرير مكتب: أنشطة المكتب، ويمكن حصرها بشخص مقيّم وبفترة زمنية</summary>
    public async Task<IActionResult> OfficeActivities(int officeId, int? personId, DateTime? fromDate, DateTime? toDate)
    {
        var office = await _db.Offices.Include(o => o.Governorate).FirstOrDefaultAsync(o => o.Id == officeId);
        if (office == null) return NotFound();

        var q = _db.Activities.Include(a => a.Governorate).Include(a => a.Section).Include(a => a.Subsid)
            .Include(a => a.People).Where(a => a.OfficeId == officeId);
        if (personId.HasValue) q = q.Where(a => a.People.Any(p => p.Id == personId));
        if (fromDate.HasValue) q = q.Where(a => a.Date >= fromDate);
        if (toDate.HasValue) q = q.Where(a => a.Date <= toDate);

        ViewBag.Office = office;
        ViewBag.Person = personId.HasValue ? await _db.People.FirstOrDefaultAsync(p => p.Id == personId) : null;
        ViewBag.From = fromDate; ViewBag.To = toDate;
        return View(await q.OrderBy(a => a.Date).ThenBy(a => a.Id).ToListAsync());
    }

    /// <summary>تقرير شخص: كل الأنشطة التي شارك بها (FindPerson)</summary>
    public async Task<IActionResult> Person(int id)
    {
        var p = await _db.People.Include(x => x.Governorate).Include(x => x.Office).Include(x => x.Activities).ThenInclude(a => a.Governorate).FirstOrDefaultAsync(x => x.Id == id);
        return p == null ? NotFound() : View(p);
    }

    /// <summary>تقرير نشاط: مقيّمو النشاط (FindActivity)</summary>
    public async Task<IActionResult> Activity(int id)
    {
        var a = await _db.Activities.Include(x => x.People).Include(x => x.Governorate).Include(x => x.Office)
            .Include(x => x.Section).Include(x => x.Subsid).Include(x => x.Plan).FirstOrDefaultAsync(x => x.Id == id);
        return a == null ? NotFound() : View(a);
    }

    /// <summary>تقرير الأنشطة بين تاريخين (printbetween)</summary>
    public async Task<IActionResult> Between(DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.Activities.Include(a => a.Governorate).Include(a => a.Office).Include(a => a.People).AsQueryable();
        if (fromDate.HasValue) q = q.Where(a => a.Date >= fromDate);
        if (toDate.HasValue) q = q.Where(a => a.Date <= toDate);
        ViewBag.From = fromDate; ViewBag.To = toDate;
        return View(await q.OrderBy(a => a.Date).ToListAsync());
    }
}
