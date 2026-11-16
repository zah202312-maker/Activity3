using ActivityApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _http;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? http = null) : base(options)
    {
        _http = http;
    }

    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Subsid> Subsids => Set<Subsid>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<PersonPhoto> PersonPhotos => Set<PersonPhoto>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<FinanceResource> FinanceResources => Set<FinanceResource>();
    public DbSet<PlanProcedure> Procedures => Set<PlanProcedure>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RelatedFile> RelatedFiles => Set<RelatedFile>();

    // ---- عزل البيانات حسب المستخدم ----
    // المستخدم العادي يرى فقط الأنشطة والخطط والأشخاص التي أضافها هو (Creator).
    // المدير يرى كل شيء. وبدون طلب HTTP (مثل التهيئة عند التشغيل) لا يُطبَّق العزل.
    private System.Security.Claims.ClaimsPrincipal? CurrentPrincipal => _http?.HttpContext?.User;
    public string? CurrentUserName => CurrentPrincipal?.Identity?.Name;
    public bool SeeAllData => _http?.HttpContext == null || CurrentPrincipal?.IsInRole("Admin") == true;

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Activity>().HasQueryFilter(x => SeeAllData || x.Creator == CurrentUserName);
        b.Entity<Plan>().HasQueryFilter(x => SeeAllData || x.Creator == CurrentUserName);
        b.Entity<Person>().HasQueryFilter(x => SeeAllData || x.Creator == CurrentUserName);
        // المكاتب والأقسام والشعب: كل مستخدم يرى ما أضافه فقط (المحافظات مشتركة)
        b.Entity<Office>().HasQueryFilter(x => SeeAllData || x.Creator == CurrentUserName);
        b.Entity<Section>().HasQueryFilter(x => SeeAllData || x.Creator == CurrentUserName);
        b.Entity<Subsid>().HasQueryFilter(x => SeeAllData || x.Creator == CurrentUserName);

        b.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        b.Entity<UserRole>().HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UserRole>().HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AppUser>().HasIndex(u => u.UserName).IsUnique();
        b.Entity<Role>().HasIndex(r => r.Name).IsUnique();

        // التسلسل الإداري: محافظة -> مكتب -> قسم -> شعبة (منع الحذف إن وُجدت عناصر فرعية)
        b.Entity<Office>().HasOne(o => o.Governorate).WithMany(g => g.Offices).HasForeignKey(o => o.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Section>().HasOne(s => s.Office).WithMany(o => o.Sections).HasForeignKey(s => s.OfficeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Subsid>().HasOne(s => s.Section).WithMany(s => s.Subsids).HasForeignKey(s => s.SectionId).OnDelete(DeleteBehavior.Restrict);

        // النشاط
        b.Entity<Activity>().HasOne(a => a.Governorate).WithMany().HasForeignKey(a => a.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Activity>().HasOne(a => a.Office).WithMany().HasForeignKey(a => a.OfficeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Activity>().HasOne(a => a.Section).WithMany().HasForeignKey(a => a.SectionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Activity>().HasOne(a => a.Subsid).WithMany().HasForeignKey(a => a.SubsidId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Activity>().HasOne(a => a.Plan).WithMany(p => p.Activities).HasForeignKey(a => a.PlanId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Activity>().HasMany(a => a.People).WithMany(p => p.Activities);

        // الخطة
        b.Entity<FinanceResource>().HasOne(f => f.Plan).WithMany(p => p.FinanceResources).HasForeignKey(f => f.PlanId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PlanProcedure>().HasOne(p => p.Plan).WithMany(p => p.Procedures).HasForeignKey(p => p.PlanId).OnDelete(DeleteBehavior.Cascade);

        // الشخص: محافظة/مكتب العمل + الصورة
        b.Entity<Person>().HasOne(x => x.Governorate).WithMany().HasForeignKey(x => x.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Person>().HasOne(x => x.Office).WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PersonPhoto>().HasKey(x => x.PersonId);
        b.Entity<PersonPhoto>().HasOne<Person>().WithOne().HasForeignKey<PersonPhoto>(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<RelatedFile>().HasIndex(f => new { f.OwnerType, f.OwnerId });
    }

    // ---- تدقيق تلقائي (Creator / LastUpdateUser / CreateDate / LastUpdate) ----
    private void ApplyAudit()
    {
        var user = _http?.HttpContext?.User?.Identity?.Name ?? "system";
        var now = DateTime.Now;
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added)
            {
                // داخل طلب HTTP: المنشئ دائماً هو المستخدم الحالي (لا يُقبل من النموذج)
                if (_http?.HttpContext != null || e.Entity.Creator == null) e.Entity.Creator = user;
                e.Entity.CreateDate = now;
            }
            else if (e.State == EntityState.Modified)
            {
                e.Property(x => x.Creator).IsModified = false;
                e.Property(x => x.CreateDate).IsModified = false;
                e.Entity.LastUpdateUser = user;
                e.Entity.LastUpdate = now;
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
