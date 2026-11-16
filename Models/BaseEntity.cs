using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

/// <summary>يقابل MainEntity / IdLong / IdDouble في Java (حقول التدقيق).</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    [Display(Name = "أنشأه")] public string? Creator { get; set; }
    public string? LastUpdateUser { get; set; }
    [Display(Name = "تاريخ الإنشاء")] public DateTime CreateDate { get; set; }
    public DateTime? LastUpdate { get; set; }
}
