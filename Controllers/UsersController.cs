using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

public class UserFormVm
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string? Password { get; set; }
    public bool Active { get; set; } = true;
    public UserType UserType { get; set; } = UserType.User;
    public List<int> RoleIds { get; set; } = new();
    public List<Role> AllRoles { get; set; } = new();
}

/// <summary>يقابل UserNewController + UserViewController</summary>
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();
    public UsersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? page, int? pageSize) =>
        View(await PagedList<AppUser>.CreateAsync(
            _db.Users.Include(u => u.UserRoles).ThenInclude(r => r.Role).OrderBy(u => u.UserName).ThenBy(u => u.Id), page, pageSize));

    public async Task<IActionResult> Create() =>
        View("Form", new UserFormVm { AllRoles = await _db.Roles.OrderBy(r => r.Name).ToListAsync() });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormVm m)
    {
        m.AllRoles = await _db.Roles.OrderBy(r => r.Name).ToListAsync();
        if (string.IsNullOrWhiteSpace(m.UserName)) ModelState.AddModelError(nameof(m.UserName), "اسم المستخدم مطلوب");
        if (string.IsNullOrWhiteSpace(m.Password) || m.Password.Length < 6) ModelState.AddModelError(nameof(m.Password), "كلمة المرور مطلوبة (6 أحرف على الأقل)");
        if (await _db.Users.AnyAsync(u => u.UserName == m.UserName)) ModelState.AddModelError(nameof(m.UserName), "اسم المستخدم مستخدم مسبقاً");
        if (!ModelState.IsValid) return View("Form", m);

        var u = new AppUser { UserName = m.UserName.Trim(), Active = m.Active, UserType = m.UserType };
        u.PasswordHash = _hasher.HashPassword(u, m.Password!);
        foreach (var rid in m.RoleIds) u.UserRoles.Add(new UserRole { RoleId = rid });
        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var u = await _db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        return View("Form", new UserFormVm
        {
            Id = u.Id, UserName = u.UserName, Active = u.Active, UserType = u.UserType,
            RoleIds = u.UserRoles.Select(r => r.RoleId).ToList(),
            AllRoles = await _db.Roles.OrderBy(r => r.Name).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserFormVm m)
    {
        var u = await _db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        m.Id = id;
        m.AllRoles = await _db.Roles.OrderBy(r => r.Name).ToListAsync();
        if (string.IsNullOrWhiteSpace(m.UserName)) ModelState.AddModelError(nameof(m.UserName), "اسم المستخدم مطلوب");
        if (await _db.Users.AnyAsync(x => x.UserName == m.UserName && x.Id != id)) ModelState.AddModelError(nameof(m.UserName), "اسم المستخدم مستخدم مسبقاً");
        if (!string.IsNullOrEmpty(m.Password) && m.Password.Length < 6) ModelState.AddModelError(nameof(m.Password), "كلمة المرور 6 أحرف على الأقل");
        if (!ModelState.IsValid) return View("Form", m);

        var oldName = u.UserName;
        var newName = m.UserName.Trim();
        if (oldName != newName)
        {
            // البيانات معزولة حسب اسم المنشئ، فننقل سجلات المستخدم إلى اسمه الجديد
            await _db.Activities.IgnoreQueryFilters().Where(x => x.Creator == oldName).ExecuteUpdateAsync(s => s.SetProperty(x => x.Creator, newName));
            await _db.Plans.IgnoreQueryFilters().Where(x => x.Creator == oldName).ExecuteUpdateAsync(s => s.SetProperty(x => x.Creator, newName));
            await _db.People.IgnoreQueryFilters().Where(x => x.Creator == oldName).ExecuteUpdateAsync(s => s.SetProperty(x => x.Creator, newName));
        }
        u.UserName = newName; u.Active = m.Active; u.UserType = m.UserType;
        if (!string.IsNullOrEmpty(m.Password)) u.PasswordHash = _hasher.HashPassword(u, m.Password);
        var toRemove = u.UserRoles.Where(x => !m.RoleIds.Contains(x.RoleId)).ToList();
        foreach (var x in toRemove) u.UserRoles.Remove(x);
        foreach (var rid in m.RoleIds.Where(rid => u.UserRoles.All(x => x.RoleId != rid)))
            u.UserRoles.Add(new UserRole { RoleId = rid, UserId = id });
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        if (u.UserName == User.Identity?.Name) { Msg.Error(TempData, "لا يمكنك حذف حسابك الحالي"); return RedirectToAction(nameof(Index)); }
        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }
}
