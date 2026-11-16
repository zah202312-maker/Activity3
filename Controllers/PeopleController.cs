using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل PersonNewController</summary>
public class PeopleController : Controller
{
    private const long MaxPhotoBytes = 2 * 1024 * 1024;
    private readonly AppDbContext _db;
    public PeopleController(AppDbContext db) => _db = db;

    private async Task LoadLists(Person? p = null)
    {
        ViewBag.Governorates = new SelectList(await _db.Governorates.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", p?.GovernorateId);
        var offices = p?.GovernorateId == null ? new List<Office>() :
            await _db.Offices.Where(o => o.GovernorateId == p.GovernorateId).OrderBy(o => o.Name).ToListAsync();
        ViewBag.Offices = new SelectList(offices, "Id", "Name", p?.OfficeId);
    }

    public async Task<IActionResult> Index(string? q, PersonCapacity? capacity, int? governorateId, int? page, int? pageSize)
    {
        var query = _db.People.Include(p => p.Governorate).Include(p => p.Office).Include(p => p.Activities).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || (p.Phone != null && p.Phone.Contains(q)) || (p.FunctionAddress != null && p.FunctionAddress.Contains(q)));
        if (capacity.HasValue) query = query.Where(p => p.Capacity == capacity);
        if (governorateId.HasValue) query = query.Where(p => p.GovernorateId == governorateId);

        ViewBag.Governorates = new SelectList(await _db.Governorates.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", governorateId);
        ViewBag.Q = q;
        return View(await PagedList<Person>.CreateAsync(query.OrderBy(p => p.Name).ThenBy(p => p.Id), page, pageSize));
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await _db.People.Include(x => x.Governorate).Include(x => x.Office).Include(x => x.Activities)
            .FirstOrDefaultAsync(x => x.Id == id);
        return p == null ? NotFound() : View(p);
    }

    /// <summary>عرض صورة الشخص</summary>
    public async Task<IActionResult> Photo(int id)
    {
        if (!await _db.People.AnyAsync(p => p.Id == id)) return NotFound(); // شخص لا يخص المستخدم الحالي
        var ph = await _db.PersonPhotos.AsNoTracking().FirstOrDefaultAsync(x => x.PersonId == id);
        return ph == null ? NotFound() : File(ph.Data, ph.ContentType);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLists();
        return View("Form", new Person());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Person m, IFormFile? photo)
    {
        ValidatePhoto(photo);
        await DropForeignOffice(m);
        if (!ModelState.IsValid) { await LoadLists(m); return View("Form", m); }

        m.HasPhoto = photo != null && photo.Length > 0;
        _db.People.Add(m);
        await _db.SaveChangesAsync();
        if (m.HasPhoto) await SavePhoto(m.Id, photo!);

        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.People.FindAsync(id);
        if (p == null) return NotFound();
        await LoadLists(p);
        return View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Person m, IFormFile? photo, bool removePhoto)
    {
        var p = await _db.People.FindAsync(id);
        if (p == null) return NotFound();
        ValidatePhoto(photo);
        await DropForeignOffice(m);
        if (!ModelState.IsValid)
        {
            m.Id = id; m.HasPhoto = p.HasPhoto;
            await LoadLists(m);
            return View("Form", m);
        }

        p.Name = m.Name; p.FunctionAddress = m.FunctionAddress; p.RolePlayed = m.RolePlayed;
        p.Education = m.Education; p.BirthDate = m.BirthDate; p.Residence = m.Residence;
        p.IsEmployee = m.IsEmployee; p.Phone = m.Phone;
        p.GovernorateId = m.GovernorateId; p.OfficeId = m.GovernorateId == null ? null : m.OfficeId;
        p.ScoutCourses = m.ScoutCourses; p.BadgesCount = m.BadgesCount; p.Capacity = m.Capacity;

        if (photo != null && photo.Length > 0)
        {
            p.HasPhoto = true;
            await _db.SaveChangesAsync();
            await SavePhoto(id, photo);
        }
        else if (removePhoto && p.HasPhoto)
        {
            var old = await _db.PersonPhotos.FindAsync(id);
            if (old != null) _db.PersonPhotos.Remove(old);
            p.HasPhoto = false;
            await _db.SaveChangesAsync();
        }
        else await _db.SaveChangesAsync();

        Msg.Updated(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.People.Include(x => x.Activities).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        p.Activities.Clear(); // فك الارتباط بالأنشطة
        _db.RelatedFiles.RemoveRange(_db.RelatedFiles.Where(f => f.OwnerType == "Person" && f.OwnerId == id));
        _db.People.Remove(p); // الصورة تُحذف تلقائياً (Cascade)
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>لا يُسمح بربط الشخص بمكتب لا يملكه المستخدم الحالي</summary>
    private async Task DropForeignOffice(Person m)
    {
        if (m.OfficeId.HasValue && !await _db.Offices.AnyAsync(o => o.Id == m.OfficeId)) m.OfficeId = null;
    }

    // ---------- الصورة ----------
    private void ValidatePhoto(IFormFile? photo)
    {
        if (photo == null || photo.Length == 0) return;
        if (!photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError("photo", "الملف المرفوع ليس صورة");
        else if (photo.Length > MaxPhotoBytes)
            ModelState.AddModelError("photo", "حجم الصورة يجب ألا يتجاوز 2 ميغابايت");
    }

    private async Task SavePhoto(int personId, IFormFile photo)
    {
        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);
        var existing = await _db.PersonPhotos.FindAsync(personId);
        if (existing == null)
            _db.PersonPhotos.Add(new PersonPhoto { PersonId = personId, ContentType = photo.ContentType, Data = ms.ToArray() });
        else { existing.ContentType = photo.ContentType; existing.Data = ms.ToArray(); }
        await _db.SaveChangesAsync();
    }
}
