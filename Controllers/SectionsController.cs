using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

public class SectionsController : Controller
{
    private readonly AppDbContext _db;
    public SectionsController(AppDbContext db) => _db = db;

    private async Task LoadLists(int? officeId = null)
    {
        ViewBag.Offices = new SelectList(
            (await _db.Offices.Include(o => o.Governorate).OrderBy(o => o.Governorate!.Name).ThenBy(o => o.Name).ToListAsync())
            .Select(o => new { o.Id, Text = $"{o.Governorate?.Name} - {o.Name}" }), "Id", "Text", officeId);
    }

    public async Task<IActionResult> Index(int? officeId, int? page, int? pageSize)
    {
        await LoadLists(officeId);
        var q = _db.Sections.Include(s => s.Office).ThenInclude(o => o!.Governorate).Include(s => s.Subsids).AsQueryable();
        if (officeId.HasValue) q = q.Where(s => s.OfficeId == officeId);
        ViewBag.OfficeId = officeId;
        return View(await PagedList<Section>.CreateAsync(q.OrderBy(s => s.Name).ThenBy(s => s.Id), page, pageSize));
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await _db.Sections.Include(x => x.Office).ThenInclude(o => o!.Governorate).Include(x => x.Subsids).FirstOrDefaultAsync(x => x.Id == id);
        return s == null ? NotFound() : View(s);
    }

    public async Task<IActionResult> Create() { await LoadLists(); return View("Form", new Section()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Section m)
    {
        if (m.OfficeId.HasValue && await _db.Sections.AnyAsync(s => s.Name == m.Name && s.OfficeId == m.OfficeId))
            ModelState.AddModelError(nameof(m.Name), "هذا القسم موجود مسبقاً في المكتب نفسه");
        if (m.OfficeId.HasValue && !await _db.Offices.AnyAsync(o => o.Id == m.OfficeId)) ModelState.AddModelError(nameof(m.OfficeId), "المكتب غير موجود");
        if (!ModelState.IsValid) { await LoadLists(m.OfficeId); return View("Form", m); }
        _db.Sections.Add(m);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Sections.FindAsync(id);
        if (s == null) return NotFound();
        await LoadLists(s.OfficeId);
        return View("Form", s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Section m)
    {
        var s = await _db.Sections.FindAsync(id);
        if (s == null) return NotFound();
        if (m.OfficeId.HasValue && await _db.Sections.AnyAsync(x => x.Name == m.Name && x.OfficeId == m.OfficeId && x.Id != id))
            ModelState.AddModelError(nameof(m.Name), "هذا القسم موجود مسبقاً في المكتب نفسه");
        if (m.OfficeId.HasValue && !await _db.Offices.AnyAsync(o => o.Id == m.OfficeId)) ModelState.AddModelError(nameof(m.OfficeId), "المكتب غير موجود");
        if (!ModelState.IsValid) { await LoadLists(m.OfficeId); return View("Form", m); }
        s.Name = m.Name; s.Code = m.Code; s.OfficeId = m.OfficeId;
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Sections.Include(x => x.Subsids).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        if (await _db.Subsids.IgnoreQueryFilters().AnyAsync(x => x.SectionId == id)) { Msg.Error(TempData, "لا يمكن حذف القسم لوجود شعب مرتبطة به"); return RedirectToAction(nameof(Index)); }
        if (await _db.Activities.IgnoreQueryFilters().AnyAsync(a => a.SectionId == id)) { Msg.Error(TempData, "لا يمكن حذف القسم لوجود أنشطة مرتبطة به"); return RedirectToAction(nameof(Index)); }
        _db.RelatedFiles.RemoveRange(_db.RelatedFiles.Where(f => f.OwnerType == "Section" && f.OwnerId == id));
        _db.Sections.Remove(s);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }
}
