using ActivityApp.Models;

namespace ActivityApp.Helpers;

public class ActivityEvaluationVm
{
    public Activity Activity { get; set; } = new();
    public List<RelatedFile> Attachments { get; set; } = new();
    public List<RelatedFile> Reports { get; set; } = new();
    public EvaluationRating? NumberOfPeopleEvaluation { get; set; }
    public EvaluationSuitability? ActivityPlaceEvaluation { get; set; }
    public ScoutUniformEvaluation? ScoutUniformEvaluation { get; set; }
    public EvaluationSuitability? FlagsEvaluation { get; set; }
    public EvaluationSuitability? ActivityReportEvaluation { get; set; }
    public EvaluationRating? ActivityOrganizationEvaluation { get; set; }
    public int? NumberOfPeopleScore { get; set; }
    public int? ActivityPlaceScore { get; set; }
    public int? ScoutUniformScore { get; set; }
    public int? FlagsScore { get; set; }
    public int? ActivityReportScore { get; set; }
    public int? ActivityOrganizationScore { get; set; }
}

public class ActivityEvaluationInputVm
{
    public EvaluationRating? NumberOfPeopleEvaluation { get; set; }
    public EvaluationSuitability? ActivityPlaceEvaluation { get; set; }
    public ScoutUniformEvaluation? ScoutUniformEvaluation { get; set; }
    public EvaluationSuitability? FlagsEvaluation { get; set; }
    public EvaluationSuitability? ActivityReportEvaluation { get; set; }
    public EvaluationRating? ActivityOrganizationEvaluation { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 20, ErrorMessage = "درجة عدد الأفراد يجب أن تكون بين 0 و20")]
    public int? NumberOfPeopleScore { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 20, ErrorMessage = "درجة مكان النشاط يجب أن تكون بين 0 و20")]
    public int? ActivityPlaceScore { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 20, ErrorMessage = "درجة الزي الكشفي يجب أن تكون بين 0 و20")]
    public int? ScoutUniformScore { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 20, ErrorMessage = "درجة الأعلام يجب أن تكون بين 0 و20")]
    public int? FlagsScore { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 5, ErrorMessage = "درجة تقرير النشاط يجب أن تكون بين 0 و5")]
    public int? ActivityReportScore { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, 15, ErrorMessage = "درجة تنظيم النشاط يجب أن تكون بين 0 و15")]
    public int? ActivityOrganizationScore { get; set; }
}
