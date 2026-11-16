using ActivityApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Helpers;

public class PendingActivityNotificationItem
{
    public int Id { get; set; }
    public DateTime? Date { get; set; }
    public string? ActivityPlace { get; set; }
}

public class PendingActivityNotificationsVm
{
    public int Count { get; set; }
    public List<PendingActivityNotificationItem> Activities { get; set; } = new();
}

public class PendingActivityNotificationsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public PendingActivityNotificationsViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!HttpContext.User.IsInRole("Admin"))
            return Content(string.Empty);

        var pendingActivities = _db.Activities.Where(activity =>
            activity.NumberOfPeopleEvaluation == null || activity.NumberOfPeopleScore == null ||
            activity.ActivityPlaceEvaluation == null || activity.ActivityPlaceScore == null ||
            activity.ScoutUniformEvaluation == null || activity.ScoutUniformScore == null ||
            activity.FlagsEvaluation == null || activity.FlagsScore == null ||
            activity.ActivityReportEvaluation == null || activity.ActivityReportScore == null ||
            activity.ActivityOrganizationEvaluation == null || activity.ActivityOrganizationScore == null);

        var model = new PendingActivityNotificationsVm
        {
            Count = await pendingActivities.CountAsync(),
            Activities = await pendingActivities
                .OrderByDescending(activity => activity.Date)
                .ThenByDescending(activity => activity.Id)
                .Take(8)
                .Select(activity => new PendingActivityNotificationItem
                {
                    Id = activity.Id,
                    Date = activity.Date,
                    ActivityPlace = activity.ActivityPlace
                })
                .ToListAsync()
        };

        return View(model);
    }
}
