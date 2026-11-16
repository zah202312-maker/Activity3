# نظام إدارة الأنشطة — ASP.NET Core MVC

تحويل مشروع Java (JSF / PrimeFaces / JPA) الموصوف في مخططات الكلاسات إلى **ASP.NET Core 8 MVC + Entity Framework Core (SQL Server)**.

## التشغيل

```bash
dotnet restore
dotnet run
```

ثم افتح الرابط الذي يظهر في الطرفية (مثل `https://localhost:5001`).

- قاعدة البيانات `ActivityDb` تُنشأ تلقائياً عند أول تشغيل مع بيانات أولية (الافتراضي: SQL Server LocalDB).
- عند تشغيل إصدار أحدث على قاعدة موجودة، تُضاف أعمدة الأنشطة والأشخاص المفقودة تلقائياً دون حذف البيانات.
- لسيرفر SQL آخر عدّل `ConnectionStrings:Default` في `appsettings.json`، مثلاً:
  `Server=MYSERVER;Database=ActivityDb;User Id=sa;Password=***;TrustServerCertificate=True`
- حساب البداية: **admin / Admin@123** (غيّر كلمة المرور فوراً من القائمة العلوية).

## مطابقة Java ← ASP.NET

| Java (الأصل) | ASP.NET (هنا) |
|---|---|
| `Entity.Location.*` (Governorat, Office, Section, Subsid) | `Models/Location.cs` + Controllers: Governorates / Offices / Sections / Subsids |
| `Entity.Activites.Activity` + `ActivityControllerNew/View` | `Models/Activity.cs` + `ActivitiesController` (بحث، حذف محدد، قوائم متسلسلة، مشاركون) |
| `Entity.Person.Person` + `PersonNewController` | `Models/Person.cs` + `PeopleController` |
| `Entity.plans.plan / finance_resource / procedures` + `PlaneNew/ViewController` | `Models/Plan.cs` + `PlansController` (إجراءات، موارد مالية، **تصدير/استيراد Excel**) |
| `Entity.User / Role` + `UserNewController`, `RoleContoller`, `LoginController`, `ChangBaswordController` | `Models/Security.cs` + `UsersController`, `RolesController`, `AccountController` (Cookie Auth + تجزئة كلمات المرور) |
| `RelatedFile.*` (مرفقات كل كيان) + `RfController` | `Models/RelatedFile.cs` + `FilesController` + مكوّن `Attachments` |
| `MainEntity / IdLong / IdDouble` (حقول التدقيق) | `BaseEntity` + تدقيق تلقائي في `AppDbContext.SaveChanges` |
| `AbstractFacade / *Facade` | `AppDbContext` (EF Core) مباشرة داخل الـ Controllers |
| `Classes.Message` (FacesMessage) | `Helpers/Msg.cs` + TempData |
| `ReportsNewController / testcontroller` (JasperReports) | `ReportsController` — صفحات HTML قابلة للطباعة (Ctrl+P) |
| `org.primefaces.poseidon.*` (قالب Demo) | **لم يُنقل** — هو قالب تجريبي من طرف ثالث، استُبدل بـ Bootstrap 5 RTL |

## ملاحظات
- `ActivityType` و`PlaceType` منقولان من ملفات Java الأصلية بالترتيب نفسه (الأسماء عربية في الواجهة وإنجليزية في الكود).
- الصلاحيات: كل الصفحات تتطلب تسجيل دخول، وصفحتا المستخدمين والأدوار للمدير فقط (`Admin`). أعلام الصلاحيات التفصيلية في `LoginController` (mapbool, storebool...) لم تُنقل لأن المخطط لا يبيّن منطقها.
- تُخزَّن مرفقات النشاط وتقاريره في قاعدة البيانات (حد 50MB للطلب). الصور والفيديوهات لها قسم مستقل عن ملفات تقرير النشاط (Word وExcel) في نموذج الإضافة وصفحة التفاصيل.
- لم أستطع تشغيل `dotnet build` في بيئة الإنشاء، فالكود غير مُجرَّب بالترجمة — إن ظهر خطأ ترجمة أرسله لي وأصلحه فوراً.
