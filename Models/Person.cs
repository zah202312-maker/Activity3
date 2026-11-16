using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

public class Person : BaseEntity
{
    [Required(ErrorMessage = "الاسم مطلوب"), StringLength(200), Display(Name = "الاسم")]
    public string Name { get; set; } = "";
    [StringLength(250), Display(Name = "الوظيفة / العنوان")] public string? FunctionAddress { get; set; }
    [StringLength(250), Display(Name = "الدور الذي قام به")] public string? RolePlayed { get; set; }
    [StringLength(200), Display(Name = "التحصيل الدراسي")] public string? Education { get; set; }
    [DataType(DataType.Date), Display(Name = "التولد")] public DateTime? BirthDate { get; set; }
    [StringLength(300), Display(Name = "محل السكن")] public string? Residence { get; set; }
    [Display(Name = "موظف")] public bool IsEmployee { get; set; }
    [StringLength(30), Display(Name = "رقم الهاتف")] public string? Phone { get; set; }

    [Display(Name = "محافظة العمل")] public int? GovernorateId { get; set; }
    public Governorate? Governorate { get; set; }
    [Display(Name = "المكتب")] public int? OfficeId { get; set; }
    public Office? Office { get; set; }

    [Range(0, 1000, ErrorMessage = "قيمة غير صحيحة"), Display(Name = "عدد الدورات الكشفية")] public int ScoutCourses { get; set; }
    [Range(0, 1000, ErrorMessage = "قيمة غير صحيحة"), Display(Name = "عدد الشارات")] public int BadgesCount { get; set; }
    [Required(ErrorMessage = "اختر الصفة"), Display(Name = "الصفة")] public PersonCapacity? Capacity { get; set; }

    /// <summary>هل للشخص صورة (الصورة نفسها في جدول PersonPhotos كي لا تُحمَّل مع القوائم)</summary>
    public bool HasPhoto { get; set; }

    public List<Activity> Activities { get; set; } = new();
}

public class PersonPhoto
{
    public int PersonId { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
