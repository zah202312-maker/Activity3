using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

public class Plan : BaseEntity
{
    [Required(ErrorMessage = "عنوان النشاط مطلوب"), StringLength(300), Display(Name = "عنوان النشاط")]
    public string ActivityTitle { get; set; } = "";
    [StringLength(250), Display(Name = "الجهة المنفذة")] public string? ExecutingAgency { get; set; }
    [StringLength(250), Display(Name = "مشرف النشاط")] public string? ActivitySupervisor { get; set; }
    [StringLength(250), Display(Name = "الفئة المستهدفة")] public string? TargetGroup { get; set; }
    [StringLength(250), Display(Name = "الجهة المعدّة")] public string? PreparedEntity { get; set; }
    [StringLength(250), Display(Name = "مكان النشاط")] public string? PlaceOfActivity { get; set; }
    [DataType(DataType.Date), Display(Name = "تاريخ إعداد الخطة")] public DateTime? DatePreparation { get; set; }
    [DataType(DataType.Date), Display(Name = "تاريخ النشاط")] public DateTime? ActivityDate { get; set; }
    [StringLength(2000), Display(Name = "الضوابط")] public string? Rules { get; set; }
    [Display(Name = "تم تصديرها")] public bool Exported { get; set; }

    public List<FinanceResource> FinanceResources { get; set; } = new();
    public List<PlanProcedure> Procedures { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();

    public override string ToString() => ActivityTitle;
}

public class FinanceResource : BaseEntity
{
    [Required(ErrorMessage = "المادة مطلوبة"), StringLength(250), Display(Name = "المادة")] public string Material { get; set; } = "";
    [Display(Name = "العدد")] public int Number { get; set; }
    [StringLength(250), Display(Name = "مكان الشراء")] public string? PlaceBuy { get; set; }
    [Display(Name = "المبلغ")] public double Amount { get; set; }
    [StringLength(250), Display(Name = "مصدر الصرف")] public string? SourceDrainage { get; set; }
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }
}

public class PlanProcedure : BaseEntity
{
    [Required(ErrorMessage = "الإجراء مطلوب"), StringLength(500), Display(Name = "الإجراء")] public string Procedure { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "التاريخ")] public DateTime? Date { get; set; }
    [StringLength(500), Display(Name = "وسيلة التنفيذ")] public string? MeanImplementation { get; set; }
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }
}
