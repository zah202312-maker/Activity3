using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>المحافظات: العرض للجميع، والإضافة للمدير فقط (التعديل والحذف غير متاحين)</summary>
public class GovernoratesController : Controller
{
    private readonly AppDbContext _db;
    public GovernoratesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? page, int? pageSize) =>
        View(await PagedList<Governorate>.CreateAsync(
            _db.Governorates.Include(g => g.Offices).OrderBy(g => g.Name).ThenBy(g => g.Id), page, pageSize));

    public async Task<IActionResult> Details(int id)
    {
        var g = await _db.Governorates.Include(x => x.Offices).FirstOrDefaultAsync(x => x.Id == id);
        return g == null ? NotFound() : View(g);
    }

    /// <summary>محافظات العراق التي لم تُضف بعد (للقائمة المنسدلة)</summary>
    private async Task LoadAvailable()
    {
        var existing = await _db.Governorates.Select(g => g.Name).ToListAsync();
        var existingSet = new HashSet<string>(existing.Select(n => n.Trim()));
        ViewBag.Available = IraqGovernorates.All.Where(n => !existingSet.Contains(n)).ToList();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadAvailable();
        return View("Form", new Governorate());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Governorate m, string? otherName)
    {
        // خيار «أخرى»: الاسم يُؤخذ من حقل الإدخال النصي
        if (m.Name == IraqGovernorates.OtherValue)
        {
            ViewBag.IsOther = true;
            m.Name = otherName?.Trim() ?? "";
            ModelState.Remove(nameof(m.Name));
            if (string.IsNullOrWhiteSpace(m.Name))
                ModelState.AddModelError(nameof(m.Name), "اكتب اسم المحافظة");
            else if (m.Name.Length > 150)
                ModelState.AddModelError(nameof(m.Name), "اسم المحافظة طويل جداً");
        }
        m.Name = m.Name?.Trim() ?? "";

        if (m.Name != "" && await _db.Governorates.AnyAsync(g => g.Name == m.Name))
            ModelState.AddModelError(nameof(m.Name), "هذه المحافظة موجودة مسبقاً");
        if (!ModelState.IsValid)
        {
            await LoadAvailable();
            ViewBag.OtherName = otherName;
            return View("Form", m);
        }
        _db.Governorates.Add(m);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }
}
