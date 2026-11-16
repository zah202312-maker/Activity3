using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

public class Governorate : BaseEntity
{
    [Required(ErrorMessage = "اسم المحافظة مطلوب"), StringLength(150), Display(Name = "اسم المحافظة")]
    public string Name { get; set; } = "";
    public List<Office> Offices { get; set; } = new();
}

public class Office : BaseEntity
{
    [Required(ErrorMessage = "اسم المكتب مطلوب"), StringLength(150), Display(Name = "اسم المكتب")]
    public string Name { get; set; } = "";
    [StringLength(50), Display(Name = "رمز المكتب")] public string? Code { get; set; }
    [Required(ErrorMessage = "اختر المحافظة"), Display(Name = "المحافظة")]
    public int? GovernorateId { get; set; }
    public Governorate? Governorate { get; set; }
    public List<Section> Sections { get; set; } = new();
}

public class Section : BaseEntity
{
    [Required(ErrorMessage = "اسم القسم مطلوب"), StringLength(150), Display(Name = "اسم القسم")]
    public string Name { get; set; } = "";
    [StringLength(50), Display(Name = "رمز القسم")] public string? Code { get; set; }
    [Required(ErrorMessage = "اختر المكتب"), Display(Name = "المكتب")]
    public int? OfficeId { get; set; }
    public Office? Office { get; set; }
    public List<Subsid> Subsids { get; set; } = new();
}

public class Subsid : BaseEntity
{
    [Required(ErrorMessage = "اسم الشعبة مطلوب"), StringLength(150), Display(Name = "اسم الشعبة")]
    public string Name { get; set; } = "";
    [StringLength(50), Display(Name = "رمز الشعبة")] public string? Code { get; set; }
    [Required(ErrorMessage = "اختر القسم"), Display(Name = "القسم")]
    public int? SectionId { get; set; }
    public Section? Section { get; set; }
}
