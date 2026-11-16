using System.Security.Claims;
using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل LoginController + ChangBaswordController</summary>
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();
    public AccountController(AppDbContext db) => _db = db;

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "") == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return View();
        }
        if (!user.Active)
        {
            ViewBag.Error = "هذا الحساب غير فعّال";
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName)
        };
        claims.AddRange(user.UserRoles.Where(r => r.Role != null).Select(r => new Claim(ClaimTypes.Role, r.Role!.Name)));
        if (user.UserType == UserType.Admin && !claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied() => View();

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(id);
        if (user == null) return RedirectToAction(nameof(Login));

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword ?? "") == PasswordVerificationResult.Failed)
            ViewBag.Error = "كلمة المرور القديمة غير صحيحة";
        else if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            ViewBag.Error = "كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف";
        else if (newPassword != confirmPassword)
            ViewBag.Error = "كلمة المرور الجديدة وتأكيدها غير متطابقتين";
        else
        {
            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();
            Msg.Info(TempData, "تم تغيير كلمة المرور بنجاح");
            return RedirectToAction("Index", "Home");
        }
        return View();
    }
}
