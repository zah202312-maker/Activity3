using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

public enum ActivityType
{
    [Display(Name = "عبادي")] Worship,
    [Display(Name = "اجتماعي")] Social,
    [Display(Name = "خدمي")] Service,
    [Display(Name = "سياسي")] Political,
    [Display(Name = "رياضي")] Sports,
    [Display(Name = "كشفي")] Scout,
    [Display(Name = "انساني")] Humanitarian,
    [Display(Name = "نسوي")] Women
}

public enum PlaceType
{
    [Display(Name = "مسجد")] Mosque,
    [Display(Name = "منزل")] Home,
    [Display(Name = "ملعب")] Stadium,
    [Display(Name = "حديقة")] Park,
    [Display(Name = "مدرسة")] School,
    [Display(Name = "شارع عام")] PublicStreet,
    [Display(Name = "دائرة حكومية")] GovernmentOffice,
    [Display(Name = "مؤسسة غير حكومية")] NonGovernmentOrg,
    [Display(Name = "مكتب حركي")] MovementOffice
}

public enum UserType
{
    [Display(Name = "مدير")] Admin,
    [Display(Name = "مستخدم")] User
}

public enum PersonCapacity
{
    [Display(Name = "حشد")] Hashd,
    [Display(Name = "عقد")] Contract,
    [Display(Name = "متفرغ")] FullTime,
    [Display(Name = "متطوع")] Volunteer
}

public enum EvaluationRating
{
    [Display(Name = "ضعيف")] Weak,
    [Display(Name = "مقبول")] Acceptable,
    [Display(Name = "متوسط")] Average,
    [Display(Name = "جيد")] Good,
    [Display(Name = "جيد جداً")] VeryGood,
    [Display(Name = "ممتاز")] Excellent
}

public enum EvaluationSuitability
{
    [Display(Name = "ملائم")] Suitable,
    [Display(Name = "غير ملائم")] Unsuitable
}

public enum ScoutUniformEvaluation
{
    [Display(Name = "زي كامل")] FullUniform,
    [Display(Name = "فقط ربطة")] NeckerchiefOnly,
    [Display(Name = "بدون زي")] NoUniform
}
