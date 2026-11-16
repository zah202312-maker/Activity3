using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل PlaneNewController + PlaneViewController (بما فيها تصدير/استيراد Excel)</summary>
public class PlansController : Controller
{
    private readonly AppDbContext _db;
    public PlansController(AppDbContext db) => _db = db;

    // ================= القائمة =================
    public async Task<IActionResult> Index(string? q, bool onlyNotExported = false, int? page = null, int? pageSize = null)
    {
        var query = _db.Plans.Include(p => p.Procedures).Include(p => p.FinanceResources).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.ActivityTitle.Contains(q) || (p.ExecutingAgency != null && p.ExecutingAgency.Contains(q)));
        if (onlyNotExported) query = query.Where(p => !p.Exported);
        ViewBag.Q = q;
        ViewBag.OnlyNotExported = onlyNotExported;
        return View(await PagedList<Plan>.CreateAsync(
            query.OrderByDescending(p => p.ActivityDate).ThenByDescending(p => p.Id), page, pageSize));
    }

    // ================= تفاصيل الخطة (الإجراءات + الموارد المالية + المرفقات) =================
    public async Task<IActionResult> Details(int id)
    {
        var p = await _db.Plans.Include(x => x.Procedures.OrderBy(i => i.Date)).Include(x => x.FinanceResources)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        ViewBag.Files = await _db.RelatedFiles.Where(f => f.OwnerType == "Plan" && f.OwnerId == id)
            .Select(f => new RelatedFile { Id = f.Id, Name = f.Name, Size = f.Size, ContentType = f.ContentType, InsertDate = f.InsertDate, OwnerType = f.OwnerType, OwnerId = f.OwnerId })
            .ToListAsync();
        return View(p);
    }

    // ================= إضافة / تعديل =================
    public IActionResult Create() => View("Form", new Plan { DatePreparation = DateTime.Today, ActivityDate = DateTime.Today });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Plan m)
    {
        if (!ModelState.IsValid) return View("Form", m);
        _db.Plans.Add(m);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Plans.FindAsync(id);
        return p == null ? NotFound() : View("Form", p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Plan m)
    {
        var p = await _db.Plans.FindAsync(id);
        if (p == null) return NotFound();
        if (!ModelState.IsValid) { m.Id = id; return View("Form", m); }
        p.ActivityTitle = m.ActivityTitle; p.ExecutingAgency = m.ExecutingAgency; p.ActivitySupervisor = m.ActivitySupervisor;
        p.TargetGroup = m.TargetGroup; p.PreparedEntity = m.PreparedEntity; p.PlaceOfActivity = m.PlaceOfActivity;
        p.DatePreparation = m.DatePreparation; p.ActivityDate = m.ActivityDate; p.Rules = m.Rules;
        p.Exported = false; // أي تعديل يجعلها بحاجة لتصدير جديد
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Details), new { id });
    }

    // ================= حذف =================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await DeleteInternal(new[] { id });
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(int[] ids)
    {
        if (ids == null || ids.Length == 0) { Msg.Error(TempData, "لم يتم تحديد أي خطة"); return RedirectToAction(nameof(Index)); }
        await DeleteInternal(ids);
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }

    private async Task DeleteInternal(int[] ids)
    {
        var plans = await _db.Plans.Include(p => p.Procedures).Include(p => p.FinanceResources).Where(p => ids.Contains(p.Id)).ToListAsync();
        _db.RelatedFiles.RemoveRange(_db.RelatedFiles.Where(f => f.OwnerType == "Plan" && ids.Contains(f.OwnerId)));
        _db.Plans.RemoveRange(plans); // الإجراءات والموارد تُحذف بالتتابع، والأنشطة يُفك ارتباطها (SetNull)
        await _db.SaveChangesAsync();
    }

    // ================= الإجراءات (procedures) =================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProcedure(int planId, string procedure, DateTime? date, string? meanImplementation)
    {
        if (!await _db.Plans.AnyAsync(p => p.Id == planId)) return NotFound();
        if (string.IsNullOrWhiteSpace(procedure)) { Msg.Error(TempData, "الإجراء مطلوب"); return RedirectToAction(nameof(Details), new { id = planId }); }
        _db.Procedures.Add(new PlanProcedure { PlanId = planId, Procedure = procedure.Trim(), Date = date, MeanImplementation = meanImplementation });
        await MarkNotExported(planId);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Details), new { id = planId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProcedure(int id)
    {
        var x = await _db.Procedures.FindAsync(id);
        if (x == null || !await _db.Plans.AnyAsync(p => p.Id == x.PlanId)) return NotFound();
        var planId = x.PlanId;
        _db.Procedures.Remove(x);
        await MarkNotExported(planId);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Details), new { id = planId });
    }

    // ================= الموارد المالية (finance_resource) =================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFinance(int planId, string material, int number, string? placeBuy, double amount, string? sourceDrainage)
    {
        if (!await _db.Plans.AnyAsync(p => p.Id == planId)) return NotFound();
        if (string.IsNullOrWhiteSpace(material)) { Msg.Error(TempData, "المادة مطلوبة"); return RedirectToAction(nameof(Details), new { id = planId }); }
        _db.FinanceResources.Add(new FinanceResource { PlanId = planId, Material = material.Trim(), Number = number, PlaceBuy = placeBuy, Amount = amount, SourceDrainage = sourceDrainage });
        await MarkNotExported(planId);
        await _db.SaveChangesAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Details), new { id = planId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFinance(int id)
    {
        var x = await _db.FinanceResources.FindAsync(id);
        if (x == null || !await _db.Plans.AnyAsync(p => p.Id == x.PlanId)) return NotFound();
        var planId = x.PlanId;
        _db.FinanceResources.Remove(x);
        await MarkNotExported(planId);
        await _db.SaveChangesAsync();
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Details), new { id = planId });
    }

    private async Task MarkNotExported(int planId)
    {
        var p = await _db.Plans.FindAsync(planId);
        if (p != null) p.Exported = false;
    }

    // ================= تصدير Excel (exportToExcel) =================
    /// <param name="ids">الخطط المحددة؛ إن تُركت فارغة تُصدَّر الكل (أو الجديدة فقط عند onlyNew)</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportExcel(int[]? ids, bool onlyNew = false)
    {
        var q = _db.Plans.Include(p => p.Procedures).Include(p => p.FinanceResources).AsQueryable();
        if (ids != null && ids.Length > 0) q = q.Where(p => ids.Contains(p.Id));
        else if (onlyNew) q = q.Where(p => !p.Exported);
        var plans = await q.OrderBy(p => p.Id).ToListAsync();
        if (plans.Count == 0) { Msg.Error(TempData, "لا توجد خطط للتصدير"); return RedirectToAction(nameof(Index)); }

        using var wb = new XLWorkbook();

        var ws = wb.AddWorksheet(SheetPlans);
        string[] h1 = { "رقم الخطة", "عنوان النشاط", "الجهة المنفذة", "مشرف النشاط", "الفئة المستهدفة", "الجهة المعدّة", "مكان النشاط", "تاريخ إعداد الخطة", "تاريخ النشاط", "الضوابط" };
        WriteHeader(ws, h1);
        int r = 2;
        foreach (var p in plans)
        {
            ws.Cell(r, 1).Value = p.Id;
            ws.Cell(r, 2).Value = p.ActivityTitle;
            ws.Cell(r, 3).Value = p.ExecutingAgency ?? "";
            ws.Cell(r, 4).Value = p.ActivitySupervisor ?? "";
            ws.Cell(r, 5).Value = p.TargetGroup ?? "";
            ws.Cell(r, 6).Value = p.PreparedEntity ?? "";
            ws.Cell(r, 7).Value = p.PlaceOfActivity ?? "";
            SetDate(ws.Cell(r, 8), p.DatePreparation);
            SetDate(ws.Cell(r, 9), p.ActivityDate);
            ws.Cell(r, 10).Value = p.Rules ?? "";
            r++;
        }
        ws.Columns().AdjustToContents();

        var wp = wb.AddWorksheet(SheetProcedures);
        WriteHeader(wp, new[] { "رقم الخطة", "الإجراء", "التاريخ", "وسيلة التنفيذ" });
        r = 2;
        foreach (var p in plans) foreach (var x in p.Procedures)
        {
            wp.Cell(r, 1).Value = p.Id; wp.Cell(r, 2).Value = x.Procedure; SetDate(wp.Cell(r, 3), x.Date); wp.Cell(r, 4).Value = x.MeanImplementation ?? ""; r++;
        }
        wp.Columns().AdjustToContents();

        var wf = wb.AddWorksheet(SheetFinance);
        WriteHeader(wf, new[] { "رقم الخطة", "المادة", "العدد", "مكان الشراء", "المبلغ", "مصدر الصرف" });
        r = 2;
        foreach (var p in plans) foreach (var x in p.FinanceResources)
        {
            wf.Cell(r, 1).Value = p.Id; wf.Cell(r, 2).Value = x.Material; wf.Cell(r, 3).Value = x.Number;
            wf.Cell(r, 4).Value = x.PlaceBuy ?? ""; wf.Cell(r, 5).Value = x.Amount; wf.Cell(r, 6).Value = x.SourceDrainage ?? ""; r++;
        }
        wf.Columns().AdjustToContents();

        foreach (var p in plans) p.Exported = true;
        await _db.SaveChangesAsync();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"plans_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // أسماء الأوراق في ملف Excel المُصدَّر
    private const string SheetPlans = "الخطط", SheetProcedures = "الإجراءات", SheetFinance = "الموارد المالية";

    private static void WriteHeader(IXLWorksheet ws, string[] headers)
    {
        ws.RightToLeft = true;                 // الورقة من اليمين إلى اليسار
        ws.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.SheetView.FreezeRows(1);            // تثبيت صف العناوين
        for (int i = 0; i < headers.Length; i++)
        {
            var c = ws.Cell(1, i + 1);
            c.Value = headers[i];
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void SetDate(IXLCell c, DateTime? d)
    {
        if (d.HasValue) { c.Value = d.Value; c.Style.DateFormat.Format = "yyyy-MM-dd"; }
    }
}
