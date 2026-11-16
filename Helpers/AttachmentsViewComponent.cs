using ActivityApp.Data;
using ActivityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Helpers;

public class AttachmentsVm
{
    public string OwnerType { get; set; } = "";
    public int OwnerId { get; set; }
    public List<RelatedFile> Files { get; set; } = new();
}

/// <summary>مكوّن المرفقات المشترك (يقابل RelatedFile controllers في Java)</summary>
public class AttachmentsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public AttachmentsViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(string ownerType, int ownerId)
    {
        var files = await _db.RelatedFiles
            .Where(f => f.OwnerType == ownerType && f.OwnerId == ownerId)
            .OrderByDescending(f => f.InsertDate)
            .Select(f => new RelatedFile { Id = f.Id, Name = f.Name, Size = f.Size, ContentType = f.ContentType, InsertDate = f.InsertDate, OwnerType = f.OwnerType, OwnerId = f.OwnerId })
            .ToListAsync();
        return View(new AttachmentsVm { OwnerType = ownerType, OwnerId = ownerId, Files = files });
    }
}
