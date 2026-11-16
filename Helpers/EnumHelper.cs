using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ActivityApp.Helpers;

public static class EnumHelper
{
    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }
}
