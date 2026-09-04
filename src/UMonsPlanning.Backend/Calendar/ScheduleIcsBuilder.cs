using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using UMonsPlanning.Backend.Contracts;
using UMonsPlanning.Pronote.Internal;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Calendar;

/// <summary>
/// Builds an iCalendar (RFC 5545) file from the courses resolved by <see cref="UMonsPlanning.Pronote.PronoteClient"/>.
///
/// PRONOTE labels (subject, groups, rooms) are in French and shown as-is in the end user's
/// calendar: see CLAUDE.md §12 (single fr-BE market, no localization infrastructure introduced
/// for this alone).
/// </summary>
public static class ScheduleIcsBuilder
{
    private const string TimeZoneId = "Europe/Brussels";
    private const string ProductId = "-//UMonsPlanning//Horaires UMONS//FR";

    public static string Build(
        ResourceDto formation,
        ResourceDto? section,
        IReadOnlyList<DayDto> days,
        IcsLayout layout,
        TimeProvider timeProvider,
        string? title = null)
    {
        var calendar = new Ical.Net.Calendar { ProductId = ProductId };
        calendar.AddTimeZone(TimeZoneId);
        calendar.Properties.Set("X-WR-CALNAME", BuildCalendarName(formation, section));
        calendar.Properties.Set("X-WR-TIMEZONE", TimeZoneId);

        CalDateTime stamp = new(timeProvider.GetUtcNow().UtcDateTime, true);

        switch (layout)
        {
            case IcsLayout.PerDay:
                string eventTitle = string.IsNullOrWhiteSpace(title) ? BuildEventTitle(formation, section) : title;
                foreach (DayDto day in days)
                {
                    calendar.Events.Add(BuildDayEvent(day, eventTitle, stamp));
                }

                break;

            case IcsLayout.PerCourse:
            default:
                foreach (CourseDto course in days.SelectMany(d => d.Courses))
                {
                    calendar.Events.Add(BuildCourseEvent(course, stamp));
                }

                break;
        }

        return new CalendarSerializer(calendar).SerializeToString()
            ?? throw new InvalidOperationException("Ical.Net returned an empty serialization result.");
    }

    private static string BuildCalendarName(ResourceDto formation, ResourceDto? section) =>
        section is null ? formation.Label : $"{formation.Label} — {section.Label}";

    /// <summary>"Dropdown #1 / Dropdown #2" title used for the per-day layout's events.</summary>
    private static string BuildEventTitle(ResourceDto formation, ResourceDto? section) =>
        section is null ? formation.Label : $"{formation.Label} / {section.Label}";

    // -----------------------------------------------------------------------
    //  Per-course layout: one VEVENT per course.
    // -----------------------------------------------------------------------

    private static CalendarEvent BuildCourseEvent(CourseDto course, CalDateTime stamp)
    {
        var calendarEvent = new CalendarEvent
        {
            Uid = $"{course.Key}@umonsplanning",
            Summary = course.Subject?.Name ?? "Cours",
            Start = ToCalDateTime(course.Start),
            End = ToCalDateTime(course.End),
            DtStamp = stamp,
            Status = course.IsCancelled ? "CANCELLED" : "CONFIRMED"
        };

        if (course.Rooms.Count > 0)
        {
            calendarEvent.Location = string.Join(", ", course.Rooms);
        }

        string? description = BuildCourseDescription(course);
        if (description is not null)
        {
            calendarEvent.Description = description;
        }

        if (!string.IsNullOrWhiteSpace(course.Category))
        {
            calendarEvent.Categories.Add(course.Category);
        }

        return calendarEvent;
    }

    private static CalDateTime ToCalDateTime(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, TimeZoneId);

    private static string? BuildCourseDescription(CourseDto course)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(course.Subject?.Code))
        {
            lines.Add($"Code : {course.Subject.Code}");
        }

        if (!string.IsNullOrWhiteSpace(course.Category))
        {
            lines.Add($"Type de cours : {course.Category}");
        }

        if (course.Groups.Count > 0)
        {
            lines.Add($"Groupes : {string.Join(", ", course.Groups)}");
        }

        if (course.Teachers.Count > 0)
        {
            lines.Add($"Enseignants : {string.Join(", ", course.Teachers)}");
        }

        if (!string.IsNullOrWhiteSpace(course.Status))
        {
            lines.Add($"Statut : {course.Status}");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : null;
    }

    // -----------------------------------------------------------------------
    //  Per-day layout: one VEVENT per day, courses listed in its description.
    // -----------------------------------------------------------------------

    // Spans from the first course's start to the last course's end, rather than an all-day event:
    // an all-day marker hides exactly the information (first/last class time) this layout exists
    // to summarize.
    private static CalendarEvent BuildDayEvent(DayDto day, string title, CalDateTime stamp) => new()
    {
        Uid = $"{day.Date:yyyy-MM-dd}-{Slug.From(title)}@umonsplanning",
        Summary = title,
        Start = ToCalDateTime(day.Courses.Min(c => c.Start)),
        End = ToCalDateTime(day.Courses.Max(c => c.End)),
        DtStamp = stamp,
        Status = "CONFIRMED",
        Description = BuildDayDescription(day.Courses),
    };

    /// <summary>
    /// Formats each course as one line: <c>Start - End -&gt; [Room] Title (Category) [Code]</c>.
    /// A cancelled course keeps its line (the required format has no cancellation marker of its
    /// own) prefixed with "(Annulé)" so the information is not silently lost.
    /// </summary>
    private static string BuildDayDescription(IReadOnlyList<CourseDto> courses) =>
        string.Join('\n', courses.OrderBy(c => c.Start).Select(BuildCourseLine));

    private static string BuildCourseLine(CourseDto course)
    {
        string prefix = course.IsCancelled ? "(Annulé) " : string.Empty;
        string location = course.Rooms.Count > 0 ? $"[{string.Join(", ", course.Rooms)}] " : string.Empty;
        string title = course.Subject?.Name ?? "Cours";
        string category = !string.IsNullOrWhiteSpace(course.Category) ? $" ({course.Category})" : string.Empty;
        string code = !string.IsNullOrWhiteSpace(course.Subject?.Code) ? $" [{course.Subject.Code}]" : string.Empty;

        return $"{prefix}{course.Start:HH'h'mm} - {course.End:HH'h'mm} -> {location}{title}{category}{code}";
    }
}
