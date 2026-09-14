using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Localization;

namespace Workit.Core.JobOpenings;

/// <summary>
/// Shapes a <see cref="JobOpening"/> for API responses: resolves the free-text content for the
/// caller's language and attaches localized enum labels.
/// </summary>
internal static class JobOpeningPresentation
{
    public static GetJobOpening.Response ToResponse(JobOpening jobOpening, ILocalizer localizer)
    {
        var content = jobOpening.ResolveContent(localizer.CurrentLanguage);
        return new GetJobOpening.Response(
            jobOpening.Id,
            jobOpening.BusinessProfileId,
            content.Title,
            content.Description,
            content.Role,
            jobOpening.Location,
            jobOpening.PayAmount,
            jobOpening.PayType,
            jobOpening.JobType,
            jobOpening.StartDate,
            jobOpening.EndDate,
            jobOpening.ShiftType,
            jobOpening.ShiftStartTime,
            jobOpening.ShiftEndTime,
            jobOpening.RequiredWorkersCount,
            jobOpening.Status,
            jobOpening.CreatedAt,
            localizer.CurrentLanguage,
            localizer.Enum(jobOpening.PayType),
            localizer.Enum(jobOpening.JobType),
            localizer.Enum(jobOpening.ShiftType),
            localizer.Enum(jobOpening.Status));
    }

    public static GetJobOpenings.Item ToListItem(JobOpening jobOpening, ILocalizer localizer)
    {
        var content = jobOpening.ResolveContent(localizer.CurrentLanguage);
        return new GetJobOpenings.Item(
            jobOpening.Id,
            jobOpening.BusinessProfileId,
            content.Title,
            content.Description,
            content.Role,
            jobOpening.Location,
            jobOpening.PayAmount,
            jobOpening.PayType,
            jobOpening.JobType,
            jobOpening.StartDate,
            jobOpening.EndDate,
            jobOpening.ShiftType,
            jobOpening.ShiftStartTime,
            jobOpening.ShiftEndTime,
            jobOpening.RequiredWorkersCount,
            jobOpening.Status,
            jobOpening.CreatedAt,
            localizer.Enum(jobOpening.PayType),
            localizer.Enum(jobOpening.JobType),
            localizer.Enum(jobOpening.ShiftType),
            localizer.Enum(jobOpening.Status));
    }

    public static GetBusinessJobOpenings.Item ToBusinessListItem(JobOpening jobOpening, ILocalizer localizer)
    {
        var content = jobOpening.ResolveContent(localizer.CurrentLanguage);
        return new GetBusinessJobOpenings.Item(
            jobOpening.Id,
            jobOpening.BusinessProfileId,
            content.Title,
            content.Description,
            content.Role,
            jobOpening.Location,
            jobOpening.PayAmount,
            jobOpening.PayType,
            jobOpening.JobType,
            jobOpening.StartDate,
            jobOpening.EndDate,
            jobOpening.ShiftType,
            jobOpening.ShiftStartTime,
            jobOpening.ShiftEndTime,
            jobOpening.RequiredWorkersCount,
            jobOpening.Status,
            jobOpening.CreatedAt,
            localizer.Enum(jobOpening.PayType),
            localizer.Enum(jobOpening.JobType),
            localizer.Enum(jobOpening.ShiftType),
            localizer.Enum(jobOpening.Status));
    }
}
