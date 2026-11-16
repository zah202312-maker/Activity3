using ActivityApp.Data;
using ActivityApp.Helpers;
using ActivityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Controllers;

/// <summary>يقابل RelatedFile / RfController (رفع - تنزيل - حذف المرفقات) لكل الكيانات</summary>
public class FilesController : Controller
{
    private static readonly HashSet<string> AllowedOwners = new() { "Activity", "ActivityReport", "Plan", "Person", "Governorate", "Office", "Section", "Subsid" };

    private readonly AppDbContext _db;
    public FilesController(AppDbContext db) => _db = db;

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(string ownerType, int ownerId, List<IFormFile> files, string? returnUrl)
    {
        if (!AllowedOwners.Contains(ownerType)) return BadRequest();
        if (!await CanAccessOwner(ownerType, ownerId)) return NotFound();
        var uploads = files?.Where(x => x.Length > 0).ToList() ?? new List<IFormFile>();
        if (uploads.Count == 0)
        {
            Msg.Error(TempData, "اختر ملفاً واحداً على الأقل");
            return SafeRedirect(returnUrl);
        }

        var mediaTypes = new Dictionary<IFormFile, string>();
        if (ownerType is "Activity" or "ActivityReport")
        {
            if (!await _db.Activities.AnyAsync(a => a.Id == ownerId)) return NotFound();

            foreach (var file in uploads)
            {
                var allowed = ownerType == "Activity"
                    ? ActivityAttachmentTypes.IsActivityMedia(file.FileName)
                    : ActivityAttachmentTypes.IsActivityReport(file.FileName);
                if (!allowed || !ActivityAttachmentTypes.TryGetContentType(file.FileName, out var contentType))
                {
                    Msg.Error(TempData, ownerType == "Activity"
                        ? "مرفقات النشاط تقبل الصور والفيديوهات فقط"
                        : "تقرير النشاط يقبل ملفات Word وExcel فقط");
                    return SafeRedirect(returnUrl);
                }

                mediaTypes[file] = contentType;
            }
        }

        foreach (var f in uploads)
        {
            using var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            var extension = Path.GetExtension(f.FileName);
            _db.RelatedFiles.Add(new RelatedFile
            {
                OwnerType = ownerType,
                OwnerId = ownerId,
                Name = Path.GetFileName(f.FileName),
                Extension = extension,
                ContentType = ownerType is "Activity" or "ActivityReport"
                    ? mediaTypes[f]
                    : string.IsNullOrEmpty(f.ContentType) ? "application/octet-stream" : f.ContentType,
                Size = f.Length,
                Data = ms.ToArray()
            });
        }
        await _db.SaveChangesAsync();
        Msg.Info(TempData, "تم رفع المرفقات");
        return SafeRedirect(returnUrl);
    }

    public async Task<IActionResult> Preview(int id)
    {
        var file = await _db.RelatedFiles.FirstOrDefaultAsync(f => f.Id == id && f.OwnerType == "Activity");
        if (file == null || !await CanAccessOwner(file.OwnerType, file.OwnerId)) return NotFound();

        if (!ActivityAttachmentTypes.TryGetPreviewContentType(file.Name, out var contentType)) return NotFound();

        return File(file.Data, contentType, enableRangeProcessing: true);
    }

    public async Task<IActionResult> Download(int id)
    {
        var f = await _db.RelatedFiles.FindAsync(id);
        if (f == null || !await CanAccessOwner(f.OwnerType, f.OwnerId)) return NotFound();
        return File(f.Data, f.ContentType, f.Name);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var f = await _db.RelatedFiles.FindAsync(id);
        if (f != null && !await CanAccessOwner(f.OwnerType, f.OwnerId)) return NotFound();
        if (f != null) { _db.RelatedFiles.Remove(f); await _db.SaveChangesAsync(); Msg.Deleted(TempData); }
        return SafeRedirect(returnUrl);
    }

    /// <summary>هل صاحب المرفق (نشاط/خطة/شخص) ظاهر للمستخدم الحالي؟ (فلاتر العزل في AppDbContext تُطبَّق هنا)</summary>
    private async Task<bool> CanAccessOwner(string ownerType, int ownerId) => ownerType switch
    {
        "Activity" or "ActivityReport" => await _db.Activities.AnyAsync(a => a.Id == ownerId),
        "Plan" => await _db.Plans.AnyAsync(p => p.Id == ownerId),
        "Person" => await _db.People.AnyAsync(p => p.Id == ownerId),
        "Office" => await _db.Offices.AnyAsync(o => o.Id == ownerId),
        "Section" => await _db.Sections.AnyAsync(s => s.Id == ownerId),
        "Subsid" => await _db.Subsids.AnyAsync(s => s.Id == ownerId),
        _ => true // المحافظات مشتركة بين الجميع
    };

    private IActionResult SafeRedirect(string? url) =>
        !string.IsNullOrEmpty(url) && Url.IsLocalUrl(url) ? Redirect(url) : RedirectToAction("Index", "Home");
}
