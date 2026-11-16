using ActivityApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Activities = await _db.Activities.CountAsync();
        ViewBag.Plans = await _db.Plans.CountAsync();
        ViewBag.People = await _db.People.CountAsync();
        ViewBag.Governorates = await _db.Governorates.CountAsync();
        ViewBag.Beneficiaries = await _db.Activities.SumAsync(a => (int?)a.NumberBeneficiaries) ?? 0;
        ViewBag.Cost = await _db.Activities.SumAsync(a => (int?)a.Cost) ?? 0;
        ViewBag.Latest = await _db.Activities.Include(a => a.Governorate).OrderByDescending(a => a.Date).Take(5).ToListAsync();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
