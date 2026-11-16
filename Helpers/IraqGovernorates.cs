namespace ActivityApp.Helpers;

/// <summary>محافظات العراق — تظهر كقائمة منسدلة في نموذج إضافة محافظة</summary>
public static class IraqGovernorates
{
    public static readonly string[] All =
    {
        "بغداد", "البصرة", "نينوى", "أربيل", "السليمانية", "دهوك", "حلبجة", "كركوك",
        "الأنبار", "صلاح الدين", "ديالى", "بابل", "كربلاء", "النجف", "القادسية",
        "واسط", "ميسان", "ذي قار", "المثنى"
    };

    /// <summary>قيمة خيار «أخرى» في القائمة (لإدخال اسم محافظة غير موجود في القائمة)</summary>
    public const string OtherValue = "__other__";
}
