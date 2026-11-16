using System.ComponentModel.DataAnnotations;
namespace ActivityApp.Models;

public class AppUser : BaseEntity
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب"), StringLength(100), Display(Name = "اسم المستخدم")]
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [Display(Name = "فعّال")] public bool Active { get; set; } = true;
    [Display(Name = "نوع المستخدم")] public UserType UserType { get; set; } = UserType.User;
    public List<UserRole> UserRoles { get; set; } = new();
}

public class Role : BaseEntity
{
    [Required(ErrorMessage = "اسم الدور مطلوب"), StringLength(100), Display(Name = "اسم الدور")] public string Name { get; set; } = "";
    [StringLength(100), Display(Name = "الاسم بالعربية")] public string? ArRole { get; set; }
    [StringLength(500), Display(Name = "ملاحظات")] public string? Note { get; set; }
    public List<UserRole> UserRoles { get; set; } = new();
}

public class UserRole
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
}
