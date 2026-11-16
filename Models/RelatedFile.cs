using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

/// <summary>يقابل RelatedFile و (ActivityRelatedFile, PlanRelatedFile, ...) في Java — مرفقات عامة تُربط بأي كيان.</summary>
public class RelatedFile : BaseEntity
{
    [Required] public string OwnerType { get; set; } = "";   // Activity | Plan | Person | Governorate | Office | Section | Subsid
    public int OwnerId { get; set; }
    [Display(Name = "اسم الملف")] public string Name { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public string? Extension { get; set; }
    public long Size { get; set; }
    public DateTime InsertDate { get; set; } = DateTime.Now;
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
