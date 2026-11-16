using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

public class OfficesController : Controller
{
    private readonly AppDbContext _db;
    public OfficesController(AppDbContext db) => _db = db;

    private async Task LoadLists(int? govId = null) =>
        ViewBag.Governorates = new SelectList(await _db.Governorates.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", govId);

    public async Task<IActionResult> Index(int? governorateId, int? page, int? pageSize)
    {
        await LoadLists(governorateId);
        var q = _db.Offices.Include(o => o.Governorate).Include(o => o.Sections).AsQueryable();
        if (governorateId.HasValue) q = q.Where(o => o.GovernorateId == governorateId);
        ViewBag.GovernorateId = governorateId;
        return View(await PagedList<Office>.CreateAsync(
            q.OrderBy(o => o.Governorate!.Name).ThenBy(o => o.Name).ThenBy(o => o.Id), page, pageSize));
    }

    public async Task<IActionResult> Details(int id)
    {
        var o = await _db.Offices.Include(x => x.Governorate).Include(x => x.Sections).FirstOrDefaultAsync(x => x.Id == id);
        return o == null ? NotFound() : View(o);
    }

    public async Task<IActionResult> Create() { await LoadLists(); return View("Form", new Office()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Office m)
    {
        if (m.GovernorateId.HasValue && await _db.Offices.AnyAsync(o => o.Name == m.Name && o.GovernorateId == m.GovernorateId))
            ModelState.AddModelError(nameof(m.Name), "هذا المكتب موجود مسبقاً في المحافظة نفسها");
        if (!ModelState.IsValid) { await LoadLists(m.GovernorateId); return View("Form", m); }
        _db.Offices.Add(m);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var o = await _db.Offices.FindAsync(id);
        if (o == null) return NotFound();
        await LoadLists(o.GovernorateId);
        return View("Form", o);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Office m)
    {
        var o = await _db.Offices.FindAsync(id);
        if (o == null) return NotFound();
        if (m.GovernorateId.HasValue && await _db.Offices.AnyAsync(x => x.Name == m.Name && x.GovernorateId == m.GovernorateId && x.Id != id))
            ModelState.AddModelError(nameof(m.Name), "هذا المكتب موجود مسبقاً في المحافظة نفسها");
        if (!ModelState.IsValid) { await LoadLists(m.GovernorateId); return View("Form", m); }
        o.Name = m.Name; o.Code = m.Code; o.GovernorateId = m.GovernorateId;
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await _db.Offices.Include(x => x.Sections).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();
        if (await _db.Sections.IgnoreQueryFilters().AnyAsync(x => x.OfficeId == id)) { Msg.Error(TempData, "لا يمكن حذف المكتب لوجود أقسام مرتبطة به"); return RedirectToAction(nameof(Index)); }
        if (await _db.Activities.IgnoreQueryFilters().AnyAsync(a => a.OfficeId == id)) { Msg.Error(TempData, "لا يمكن حذف المكتب لوجود أنشطة مرتبطة به"); return RedirectToAction(nameof(Index)); }
        if (await _db.People.IgnoreQueryFilters().AnyAsync(a => a.OfficeId == id)) { Msg.Error(TempData, "لا يمكن حذف المكتب لوجود أشخاص مرتبطين به"); return RedirectToAction(nameof(Index)); }
        _db.RelatedFiles.RemoveRange(_db.RelatedFiles.Where(f => f.OwnerType == "Office" && f.OwnerId == id));
        _db.Offices.Remove(o);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }
}
