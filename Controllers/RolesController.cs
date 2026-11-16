using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل RoleContoller</summary>
[Authorize(Roles = "Admin")]
public class RolesController : Controller
{
    private readonly AppDbContext _db;
    public RolesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? page, int? pageSize) =>
        View(await PagedList<Role>.CreateAsync(
            _db.Roles.Include(r => r.UserRoles).OrderBy(r => r.Name).ThenBy(r => r.Id), page, pageSize));

    public IActionResult Create() => View("Form", new Role());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Role m)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == m.Name)) ModelState.AddModelError(nameof(m.Name), "الدور موجود مسبقاً");
        if (!ModelState.IsValid) return View("Form", m);
        _db.Roles.Add(m);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _db.Roles.FindAsync(id);
        return r == null ? NotFound() : View("Form", r);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Role m)
    {
        var r = await _db.Roles.FindAsync(id);
        if (r == null) return NotFound();
        if (await _db.Roles.AnyAsync(x => x.Name == m.Name && x.Id != id)) ModelState.AddModelError(nameof(m.Name), "الدور موجود مسبقاً");
        if (!ModelState.IsValid) return View("Form", m);
        r.Name = m.Name; r.ArRole = m.ArRole; r.Note = m.Note;
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.Roles.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Name == "Admin") { Msg.Error(TempData, "لا يمكن حذف دور مدير النظام"); return RedirectToAction(nameof(Index)); }
        if (r.UserRoles.Any()) { Msg.Error(TempData, "لا يمكن حذف دور مرتبط بمستخدمين"); return RedirectToAction(nameof(Index)); }
        _db.Roles.Remove(r);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }
}
