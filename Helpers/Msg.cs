using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ActivityApp.Helpers;

/// <summary>يقابل Classes.Message (FacesMessage) في Java.</summary>
public static class Msg
{
    public static void Saved(ITempDataDictionary t) => t["Ok"] = "تم الحفظ بنجاح";
    public static void Added(ITempDataDictionary t) => t["Ok"] = "تمت الإضافة بنجاح";
    public static void Updated(ITempDataDictionary t) => t["Ok"] = "تم التعديل بنجاح";
    public static void Deleted(ITempDataDictionary t) => t["Ok"] = "تم الحذف بنجاح";
    public static void Error(ITempDataDictionary t, string m) => t["Err"] = m;
    public static void Info(ITempDataDictionary t, string m) => t["Ok"] = m;
}
