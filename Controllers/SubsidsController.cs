using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

public class SubsidsController : Controller
{
    private readonly AppDbContext _db;
    public SubsidsController(AppDbContext db) => _db = db;

    private async Task LoadLists(int? sectionId = null)
    {
        var list = await _db.Sections.Include(s => s.Office).ThenInclude(o => o!.Governorate)
            .OrderBy(s => s.Office!.Governorate!.Name).ThenBy(s => s.Office!.Name).ThenBy(s => s.Name).ToListAsync();
        ViewBag.Sections = new SelectList(
            list.Select(s => new { s.Id, Text = $"{s.Office?.Governorate?.Name} - {s.Office?.Name} - {s.Name}" }), "Id", "Text", sectionId);
    }

    public async Task<IActionResult> Index(int? sectionId, int? page, int? pageSize)
    {
        await LoadLists(sectionId);
        var q = _db.Subsids.Include(s => s.Section).ThenInclude(x => x!.Office).ThenInclude(o => o!.Governorate).AsQueryable();
        if (sectionId.HasValue) q = q.Where(s => s.SectionId == sectionId);
        ViewBag.SectionId = sectionId;
        return View(await PagedList<Subsid>.CreateAsync(q.OrderBy(s => s.Name).ThenBy(s => s.Id), page, pageSize));
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await _db.Subsids.Include(x => x.Section).ThenInclude(x => x!.Office).ThenInclude(o => o!.Governorate).FirstOrDefaultAsync(x => x.Id == id);
        return s == null ? NotFound() : View(s);
    }

    public async Task<IActionResult> Create() { await LoadLists(); return View("Form", new Subsid()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subsid m)
    {
        if (m.SectionId.HasValue && await _db.Subsids.AnyAsync(s => s.Name == m.Name && s.SectionId == m.SectionId))
            ModelState.AddModelError(nameof(m.Name), "هذه الشعبة موجودة مسبقاً في القسم نفسه");
        if (m.SectionId.HasValue && !await _db.Sections.AnyAsync(s => s.Id == m.SectionId)) ModelState.AddModelError(nameof(m.SectionId), "القسم غير موجود");
        if (!ModelState.IsValid) { await LoadLists(m.SectionId); return View("Form", m); }
        _db.Subsids.Add(m);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Subsids.FindAsync(id);
        if (s == null) return NotFound();
        await LoadLists(s.SectionId);
        return View("Form", s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Subsid m)
    {
        var s = await _db.Subsids.FindAsync(id);
        if (s == null) return NotFound();
        if (m.SectionId.HasValue && await _db.Subsids.AnyAsync(x => x.Name == m.Name && x.SectionId == m.SectionId && x.Id != id))
            ModelState.AddModelError(nameof(m.Name), "هذه الشعبة موجودة مسبقاً في القسم نفسه");
        if (m.SectionId.HasValue && !await _db.Sections.AnyAsync(s => s.Id == m.SectionId)) ModelState.AddModelError(nameof(m.SectionId), "القسم غير موجود");
        if (!ModelState.IsValid) { await LoadLists(m.SectionId); return View("Form", m); }
        s.Name = m.Name; s.Code = m.Code; s.SectionId = m.SectionId;
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Subsids.FindAsync(id);
        if (s == null) return NotFound();
        if (await _db.Activities.IgnoreQueryFilters().AnyAsync(a => a.SubsidId == id)) { Msg.Error(TempData, "لا يمكن حذف الشعبة لوجود أنشطة مرتبطة بها"); return RedirectToAction(nameof(Index)); }
        _db.RelatedFiles.RemoveRange(_db.RelatedFiles.Where(f => f.OwnerType == "Subsid" && f.OwnerId == id));
        _db.Subsids.Remove(s);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }
}
