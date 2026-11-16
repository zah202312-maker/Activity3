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
        return View();
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
