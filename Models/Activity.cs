using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ActivityApp.Models;

public class Activity : BaseEntity
{
    [Required(ErrorMessage = "التاريخ مطلوب"), DataType(DataType.Date), Display(Name = "التاريخ")]
    public DateTime? Date { get; set; }
    [Display(Name = "الكلفة")] public int? Cost { get; set; }
    [Display(Name = "نوع النشاط")] public ActivityType ActivityType { get; set; }
    [StringLength(250), Display(Name = "مكان النشاط")] public string? ActivityPlace { get; set; }
    [StringLength(250), Display(Name = "اسم الفوج")] public string? TroopName { get; set; }
    [Display(Name = "نوع المكان")] public PlaceType PlaceType { get; set; }
    [StringLength(500), Display(Name = "الموارد")] public string? Resources { get; set; }
    [Display(Name = "عدد الأفراد")] public int? NumberBeneficiaries { get; set; }
    [StringLength(500), Display(Name = "ملاحظات المستفيدين")] public string? NoteBeneficiaries { get; set; }
    [StringLength(1000), Display(Name = "ملاحظات")] public string? Note { get; set; }

    [Display(Name = "تقييم عدد الأفراد")] public EvaluationRating? NumberOfPeopleEvaluation { get; set; }
    [Display(Name = "ملاءمة مكان النشاط")] public EvaluationSuitability? ActivityPlaceEvaluation { get; set; }
    [Display(Name = "تقييم الزي الكشفي")] public ScoutUniformEvaluation? ScoutUniformEvaluation { get; set; }
    [Display(Name = "ملاءمة الأعلام")] public EvaluationSuitability? FlagsEvaluation { get; set; }
    [Display(Name = "ملاءمة تقرير النشاط")] public EvaluationSuitability? ActivityReportEvaluation { get; set; }
    [Display(Name = "تنظيم النشاط")] public EvaluationRating? ActivityOrganizationEvaluation { get; set; }

    [Range(0, 20)] public int? NumberOfPeopleScore { get; set; }
    [Range(0, 20)] public int? ActivityPlaceScore { get; set; }
    [Range(0, 20)] public int? ScoutUniformScore { get; set; }
    [Range(0, 20)] public int? FlagsScore { get; set; }
    [Range(0, 5)] public int? ActivityReportScore { get; set; }
    [Range(0, 15)] public int? ActivityOrganizationScore { get; set; }

    [NotMapped]
    public bool EvaluationCompleted =>
        NumberOfPeopleEvaluation.HasValue && NumberOfPeopleScore.HasValue &&
        ActivityPlaceEvaluation.HasValue && ActivityPlaceScore.HasValue &&
        ScoutUniformEvaluation.HasValue && ScoutUniformScore.HasValue &&
        FlagsEvaluation.HasValue && FlagsScore.HasValue &&
        ActivityReportEvaluation.HasValue && ActivityReportScore.HasValue &&
        ActivityOrganizationEvaluation.HasValue && ActivityOrganizationScore.HasValue;

    [NotMapped]
    public string? OverallEvaluation
    {
        get
        {
            if (!EvaluationCompleted) return null;

            var total = NumberOfPeopleScore!.Value + ActivityPlaceScore!.Value +
                ScoutUniformScore!.Value + FlagsScore!.Value +
                ActivityReportScore!.Value + ActivityOrganizationScore!.Value;

            return total < 50 ? "ضعيف"
                : total < 60 ? "مقبول"
                : total < 70 ? "متوسط"
                : total < 80 ? "جيد"
                : total < 90 ? "جيد جداً"
                : "ممتاز";
        }
    }

    [Display(Name = "المحافظة")] public int? GovernorateId { get; set; }
    public Governorate? Governorate { get; set; }
    [Display(Name = "المكتب")] public int? OfficeId { get; set; }
    public Office? Office { get; set; }
    [Display(Name = "القسم")] public int? SectionId { get; set; }
    public Section? Section { get; set; }
    [Display(Name = "الشعبة")] public int? SubsidId { get; set; }
    public Subsid? Subsid { get; set; }
    [Display(Name = "الخطة")] public int? PlanId { get; set; }
    public Plan? Plan { get; set; }

    public List<Person> People { get; set; } = new();
    [NotMapped] public List<int> PersonIds { get; set; } = new();
}
