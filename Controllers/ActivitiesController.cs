using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل ActivitiesControllerView + ActivityControllerNew + ActivityRelatedFileController</summary>
public class ActivitiesController : Controller
{
    private readonly AppDbContext _db;
    public ActivitiesController(AppDbContext db) => _db = db;

    private async Task LoadLists(Activity? a = null)
    {
        ViewBag.Governorates = new SelectList(await _db.Governorates.OrderBy(x => x.Name).ToListAsync(), "Id", "Name", a?.GovernorateId);
        ViewBag.Offices = new SelectList(a?.GovernorateId == null ? new List<Office>() :
            await _db.Offices.Where(x => x.GovernorateId == a.GovernorateId).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", a?.OfficeId);
        ViewBag.Sections = new SelectList(a?.OfficeId == null ? new List<Section>() :
            await _db.Sections.Where(x => x.OfficeId == a.OfficeId).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", a?.SectionId);
        ViewBag.Subsids = new SelectList(a?.SectionId == null ? new List<Subsid>() :
            await _db.Subsids.Where(x => x.SectionId == a.SectionId).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", a?.SubsidId);
        ViewBag.Plans = new SelectList(await _db.Plans.OrderByDescending(x => x.ActivityDate).ToListAsync(), "Id", "ActivityTitle", a?.PlanId);
        ViewBag.People = await _db.People.OrderBy(x => x.Name).ToListAsync();
    }

    // ---------- القائمة + البحث ----------
    public async Task<IActionResult> Index(int? id, DateTime? fromDate, DateTime? toDate, ActivityType? type, int? governorateId, int? page, int? pageSize)
    {
        var q = _db.Activities.Include(a => a.Governorate).Include(a => a.Office).Include(a => a.People).AsQueryable();
        if (id.HasValue) q = q.Where(a => a.Id == id);
        if (fromDate.HasValue) q = q.Where(a => a.Date >= fromDate);
        if (toDate.HasValue) q = q.Where(a => a.Date <= toDate);
        if (type.HasValue) q = q.Where(a => a.ActivityType == type);
        if (governorateId.HasValue) q = q.Where(a => a.GovernorateId == governorateId);

        ViewBag.Governorates = new SelectList(await _db.Governorates.OrderBy(x => x.Name).ToListAsync(), "Id", "Name", governorateId);
        return View(await PagedList<Activity>.CreateAsync(q.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id), page, pageSize));
    }

    public async Task<IActionResult> Details(int id)
    {
        var a = await _db.Activities.Include(x => x.Governorate).Include(x => x.Office).Include(x => x.Section)
            .Include(x => x.Subsid).Include(x => x.Plan).Include(x => x.People)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        ViewBag.Files = await _db.RelatedFiles.Where(f => f.OwnerType == "Activity" && f.OwnerId == id)
            .Select(f => new RelatedFile { Id = f.Id, Name = f.Name, Size = f.Size, ContentType = f.ContentType, InsertDate = f.InsertDate, OwnerType = f.OwnerType, OwnerId = f.OwnerId })
            .ToListAsync();
        return View(a);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Evaluation(DateTime? fromDate, DateTime? toDate, bool? evaluationCompleted, int? governorateId, int? page, int? pageSize)
    {
        var query = _db.Activities
            .Include(a => a.People)
            .Include(a => a.Governorate)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.Date >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(a => a.Date < toDate.Value.Date.AddDays(1));
        if (governorateId.HasValue)
            query = query.Where(a => a.GovernorateId == governorateId);
        if (evaluationCompleted == true)
        {
            query = query.Where(a =>
                a.NumberOfPeopleEvaluation.HasValue && a.NumberOfPeopleScore.HasValue &&
                a.ActivityPlaceEvaluation.HasValue && a.ActivityPlaceScore.HasValue &&
                a.ScoutUniformEvaluation.HasValue && a.ScoutUniformScore.HasValue &&
                a.FlagsEvaluation.HasValue && a.FlagsScore.HasValue &&
                a.ActivityReportEvaluation.HasValue && a.ActivityReportScore.HasValue &&
                a.ActivityOrganizationEvaluation.HasValue && a.ActivityOrganizationScore.HasValue);
        }
        else if (evaluationCompleted == false)
        {
            query = query.Where(a =>
                !a.NumberOfPeopleEvaluation.HasValue || !a.NumberOfPeopleScore.HasValue ||
                !a.ActivityPlaceEvaluation.HasValue || !a.ActivityPlaceScore.HasValue ||
                !a.ScoutUniformEvaluation.HasValue || !a.ScoutUniformScore.HasValue ||
                !a.FlagsEvaluation.HasValue || !a.FlagsScore.HasValue ||
                !a.ActivityReportEvaluation.HasValue || !a.ActivityReportScore.HasValue ||
                !a.ActivityOrganizationEvaluation.HasValue || !a.ActivityOrganizationScore.HasValue);
        }

        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.EvaluationCompleted = evaluationCompleted?.ToString().ToLowerInvariant();
        ViewBag.Governorates = new SelectList(await _db.Governorates.OrderBy(x => x.Name).ToListAsync(), "Id", "Name", governorateId);

        var activities = await PagedList<Activity>.CreateAsync(
            query.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id), page, pageSize);
        return View("EvaluationIndex", activities);
    }

    /// <summary>
    /// تنبيه المدير بالأنشطة الجديدة: تُستدعى دورياً من الصفحة (wwwroot/js/site.js).
    /// afterId &lt; 0 → يُرجع فقط آخر رقم (نقطة البداية) دون عناصر.
    /// </summary>
    [Authorize(Roles = "Admin"), HttpGet]
    public async Task<IActionResult> NewActivities(int afterId = -1)
    {
        var me = User.Identity?.Name;
        var latestId = await _db.Activities.MaxAsync(a => (int?)a.Id) ?? 0;
        if (afterId < 0) return Json(new { latestId, items = Array.Empty<object>() });

        var items = await _db.Activities
            .Where(a => a.Id > afterId && a.Creator != me) // ما أضافه المدير نفسه لا يُنبَّه عنه
            .OrderBy(a => a.Id)
            .Take(10)
            .Select(a => new { a.Id, a.ActivityPlace, a.Date, a.Creator })
            .ToListAsync();
        return Json(new
        {
            latestId,
            items = items.Select(a => new
            {
                id = a.Id,
                place = a.ActivityPlace,
                date = a.Date?.ToString("yyyy-MM-dd"),
                creator = a.Creator,
                url = Url.Action(nameof(Details), new { id = a.Id })
            })
        });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EvaluationDetails(int id)
    {
        var activity = await _db.Activities
            .Include(a => a.People)
            .Include(a => a.Governorate)
            .Include(a => a.Office)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (activity == null) return NotFound();

        var files = await _db.RelatedFiles
            .Where(file => file.OwnerId == id &&
                (file.OwnerType == "Activity" || file.OwnerType == "ActivityReport"))
            .OrderByDescending(file => file.InsertDate)
            .Select(file => new RelatedFile
            {
                Id = file.Id,
                OwnerType = file.OwnerType,
                Name = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                InsertDate = file.InsertDate
            })
            .ToListAsync();

        return View(new ActivityEvaluationVm
        {
            Activity = activity,
            Attachments = files.Where(file => file.OwnerType == "Activity").ToList(),
            Reports = files.Where(file => file.OwnerType == "ActivityReport").ToList(),
            NumberOfPeopleEvaluation = activity.NumberOfPeopleEvaluation,
            ActivityPlaceEvaluation = activity.ActivityPlaceEvaluation,
            ScoutUniformEvaluation = activity.ScoutUniformEvaluation,
            FlagsEvaluation = activity.FlagsEvaluation,
            ActivityReportEvaluation = activity.ActivityReportEvaluation,
            ActivityOrganizationEvaluation = activity.ActivityOrganizationEvaluation,
            NumberOfPeopleScore = activity.NumberOfPeopleScore,
            ActivityPlaceScore = activity.ActivityPlaceScore,
            ScoutUniformScore = activity.ScoutUniformScore,
            FlagsScore = activity.FlagsScore,
            ActivityReportScore = activity.ActivityReportScore,
            ActivityOrganizationScore = activity.ActivityOrganizationScore
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> EvaluationDetails(int id, ActivityEvaluationInputVm evaluation)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id);
        if (activity == null) return NotFound();
        if (!ModelState.IsValid ||
            !IsDefined(evaluation.NumberOfPeopleEvaluation) ||
            !IsDefined(evaluation.ActivityPlaceEvaluation) ||
            !IsDefined(evaluation.ScoutUniformEvaluation) ||
            !IsDefined(evaluation.FlagsEvaluation) ||
            !IsDefined(evaluation.ActivityReportEvaluation) ||
            !IsDefined(evaluation.ActivityOrganizationEvaluation))
        {
            Msg.Error(TempData, "تأكد من صحة الاختيارات والدرجات ضمن الحدود المحددة");
            return RedirectToAction(nameof(EvaluationDetails), new { id });
        }

        activity.NumberOfPeopleEvaluation = evaluation.NumberOfPeopleEvaluation;
        activity.ActivityPlaceEvaluation = evaluation.ActivityPlaceEvaluation;
        activity.ScoutUniformEvaluation = evaluation.ScoutUniformEvaluation;
        activity.FlagsEvaluation = evaluation.FlagsEvaluation;
        activity.ActivityReportEvaluation = evaluation.ActivityReportEvaluation;
        activity.ActivityOrganizationEvaluation = evaluation.ActivityOrganizationEvaluation;
        activity.NumberOfPeopleScore = evaluation.NumberOfPeopleScore;
        activity.ActivityPlaceScore = evaluation.ActivityPlaceScore;
        activity.ScoutUniformScore = evaluation.ScoutUniformScore;
        activity.FlagsScore = evaluation.FlagsScore;
        activity.ActivityReportScore = evaluation.ActivityReportScore;
        activity.ActivityOrganizationScore = evaluation.ActivityOrganizationScore;
        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(EvaluationDetails), new { id });
    }

    /// <summary>لا يُسمح بربط النشاط بخطة أو مكتب أو قسم أو شعبة لا يملكها المستخدم الحالي</summary>
    private async Task DropForeignPlan(Activity m)
    {
        if (m.PlanId.HasValue && !await _db.Plans.AnyAsync(p => p.Id == m.PlanId)) m.PlanId = null;
        if (m.OfficeId.HasValue && !await _db.Offices.AnyAsync(x => x.Id == m.OfficeId)) m.OfficeId = null;
        if (m.SectionId.HasValue && !await _db.Sections.AnyAsync(x => x.Id == m.SectionId)) m.SectionId = null;
        if (m.SubsidId.HasValue && !await _db.Subsids.AnyAsync(x => x.Id == m.SubsidId)) m.SubsidId = null;
    }

    private static bool IsDefined<TEnum>(TEnum? value) where TEnum : struct, Enum =>
        !value.HasValue || Enum.IsDefined(value.Value);

    // ---------- إضافة ----------
    public async Task<IActionResult> Create()
    {
        var a = new Activity { Date = DateTime.Today };
        await LoadLists(a);
        return View("Form", a);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        Activity m,
        List<IFormFile>? activityFiles,
        List<IFormFile>? reportFiles)
    {
        var mediaUploads = activityFiles?.Where(file => file.Length > 0).ToList() ?? new List<IFormFile>();
        var reportUploads = reportFiles?.Where(file => file.Length > 0).ToList() ?? new List<IFormFile>();
        if (mediaUploads.Any(file => !ActivityAttachmentTypes.IsActivityMedia(file.FileName)))
            ModelState.AddModelError(string.Empty, "مرفقات النشاط تقبل الصور والفيديوهات فقط");
        if (reportUploads.Any(file => !ActivityAttachmentTypes.IsActivityReport(file.FileName)))
            ModelState.AddModelError(string.Empty, "تقرير النشاط يقبل ملفات Word وExcel فقط");
        await DropForeignPlan(m);

        if (!ModelState.IsValid) { await LoadLists(m); return View("Form", m); }

        var attachmentData = new List<(IFormFile File, string OwnerType, string ContentType, byte[] Data)>();
        foreach (var (uploads, ownerType) in new[]
        {
            (mediaUploads, "Activity"),
            (reportUploads, "ActivityReport")
        })
        {
            foreach (var file in uploads)
            {
                ActivityAttachmentTypes.TryGetContentType(file.FileName, out var contentType);
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                attachmentData.Add((file, ownerType, contentType, stream.ToArray()));
            }
        }

        m.People = await _db.People.Where(p => m.PersonIds.Contains(p.Id)).ToListAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Activities.Add(m);
        await _db.SaveChangesAsync();

        foreach (var attachment in attachmentData)
        {
            _db.RelatedFiles.Add(new RelatedFile
            {
                OwnerType = attachment.OwnerType,
                OwnerId = m.Id,
                Name = Path.GetFileName(attachment.File.FileName),
                Extension = Path.GetExtension(attachment.File.FileName),
                ContentType = attachment.ContentType,
                Size = attachment.File.Length,
                Data = attachment.Data
            });
        }

        if (attachmentData.Count > 0) await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        Msg.Added(TempData);
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    // ---------- تعديل ----------
    public async Task<IActionResult> Edit(int id)
    {
        var a = await _db.Activities.Include(x => x.People).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        a.PersonIds = a.People.Select(p => p.Id).ToList();
        await LoadLists(a);
        return View("Form", a);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Activity m)
    {
        var a = await _db.Activities.Include(x => x.People).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        await DropForeignPlan(m);
        if (!ModelState.IsValid) { m.Id = id; await LoadLists(m); return View("Form", m); }

        a.Date = m.Date; a.Cost = m.Cost; a.ActivityType = m.ActivityType; a.ActivityPlace = m.ActivityPlace;
        a.PlaceType = m.PlaceType; a.Resources = m.Resources; a.NumberBeneficiaries = m.NumberBeneficiaries;
        a.NoteBeneficiaries = m.NoteBeneficiaries; a.Note = m.Note;
        a.GovernorateId = m.GovernorateId; a.OfficeId = m.OfficeId; a.SectionId = m.SectionId;
        a.SubsidId = m.SubsidId; a.PlanId = m.PlanId;

        a.People.Clear();
        a.People.AddRange(await _db.People.Where(p => m.PersonIds.Contains(p.Id)).ToListAsync());

        await _db.SaveChangesAsync();
        Msg.Updated(TempData);
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- حذف ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await DeleteInternal(new[] { id });
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>يقابل deleteSelected</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(int[] ids)
    {
        if (ids == null || ids.Length == 0) { Msg.Error(TempData, "لم يتم تحديد أي نشاط"); return RedirectToAction(nameof(Index)); }
        await DeleteInternal(ids);
        Msg.Deleted(TempData);
        return RedirectToAction(nameof(Index));
    }

    private async Task DeleteInternal(int[] ids)
    {
        var items = await _db.Activities.Include(a => a.People).Where(a => ids.Contains(a.Id)).ToListAsync();
        foreach (var a in items) a.People.Clear();
        _db.RelatedFiles.RemoveRange(_db.RelatedFiles.Where(f =>
            (f.OwnerType == "Activity" || f.OwnerType == "ActivityReport") && ids.Contains(f.OwnerId)));
        _db.Activities.RemoveRange(items);
        await _db.SaveChangesAsync();
    }
}
