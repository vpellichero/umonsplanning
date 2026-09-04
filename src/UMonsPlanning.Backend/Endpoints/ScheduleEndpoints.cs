using System.Text;
using Microsoft.AspNetCore.Mvc;
using UMonsPlanning.Backend.Calendar;
using UMonsPlanning.Backend.Contracts;
using UMonsPlanning.Backend.Validation;
using UMonsPlanning.Pronote;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Endpoints;

/// <summary>Schedule: JSON view of a week, academic calendar, and iCalendar export.</summary>
public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/calendar", async (IPronoteClient client, CancellationToken ct) =>
                Results.Ok(await client.GetCalendarAsync(ct)))
           .CacheOutput(p => p.Expire(TimeSpan.FromHours(6)))
           .WithName("GetCalendar")
           .WithSummary("Academic calendar: PRONOTE week numbering and grid parameters.")
           .Produces<CalendarDto>();

        app.MapGet("/api/schedule", async (
                [FromQuery] string formation,
                [FromQuery] int week,
                [FromQuery] string? section,
                IPronoteClient client,
                CancellationToken ct) =>
                Results.Ok(await client.GetScheduleAsync(formation, section, week, ct)))
           .CacheOutput(p => p.Expire(TimeSpan.FromMinutes(10))
                              .SetVaryByQuery("formation", "section", "week"))
           .WithName("GetSchedule")
           .WithSummary("Structured schedule of a week, as JSON.")
           .WithDescription("""
                formation : slug or exact label returned by /api/formations
                section   : slug or exact label returned by /api/formations/{formation}/sections (optional)
                week      : PRONOTE week number (1 = the week of the academic year's first Monday)
                """)
           .Produces<ScheduleDto>();

        app.MapGet("/api/weeks/by-date/{date}", async (
                DateOnly date,
                IPronoteClient client,
                CancellationToken ct) =>
                Results.Ok(new { date, week = await client.GetWeekNumberAsync(date, ct) }))
           .WithName("GetWeekByDate")
           .WithSummary("Translates a date into a PRONOTE week number.")
           .WithDescription(
               "The returned number is always between 1 and the last week of the academic year: " +
               "a date before the term starts returns 1, a date after it ends returns the last week.");

        app.MapGet("/api/schedule.ics", async (
                [AsParameters] ScheduleIcsQuery query,
                IPronoteClient client,
                TimeProvider timeProvider,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                string ics = await BuildIcsAsync(query, client, timeProvider, ct).ConfigureAwait(false);
                httpContext.Response.Headers.ContentDisposition = "inline; filename=\"umons-planning.ics\"";
                return Results.Text(ics, "text/calendar", Encoding.UTF8);
            })
           .AddEndpointFilter<ValidationFilter<ScheduleIcsQuery>>()
           .CacheOutput(p => p.Expire(TimeSpan.FromMinutes(30))
                              .SetVaryByQuery("formation", "section", "week", "date", "start", "end", "layout"))
           .WithName("GetScheduleIcs")
           .WithSummary("iCalendar export (.ics) — subscribe to it as-is in a calendar application.")
           .WithDescription("""
                Only one selection mode at a time:
                  week              : one specific week (PRONOTE number)
                  date              : one specific week, derived from the date
                  start and/or end  : every week with courses in this period (bounds included) ;
                                      start alone goes up to the last available week, end alone starts
                                      from the first available week
                  (none of the three) : every week with courses in the academic year

                The URL meant for a calendar subscription (always up to date) omits these parameters
                or only provides start and/or end ; week and date are mostly used to preview one
                specific week.

                layout : "PerCourse" (default) — one event per course ;
                         "PerDay" — one event per day, course details listed in its description.
                """)
           .Produces<string>(contentType: "text/calendar");

        return app;
    }

    private static async Task<string> BuildIcsAsync(
        ScheduleIcsQuery query,
        IPronoteClient client,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<int> weeks = await ResolveWeeksAsync(query, client, cancellationToken).ConfigureAwait(false);

        ResourceDto formation;
        ResourceDto? section;
        var days = new List<DayDto>();

        if (weeks.Count == 0)
        {
            // No week with courses in the requested range: a single call (always valid, since
            // 1 <= WeekCount) is enough to validate the formation/section and know their label ;
            // its courses are not included since they fall outside the requested range.
            ScheduleDto probe = await client.GetScheduleAsync(query.Formation, query.Section, week: 1, cancellationToken)
                .ConfigureAwait(false);
            formation = probe.Formation;
            section = probe.Section;
        }
        else
        {
            ScheduleDto? last = null;
            foreach (int week in weeks)
            {
                last = await client.GetScheduleAsync(query.Formation, query.Section, week, cancellationToken)
                    .ConfigureAwait(false);
                days.AddRange(last.Days);
            }

            formation = last!.Formation;
            section = last.Section;
        }

        return ScheduleIcsBuilder.Build(formation, section, days, query.Layout ?? IcsLayout.PerCourse, timeProvider);
    }

    private static async Task<IReadOnlyList<int>> ResolveWeeksAsync(
        ScheduleIcsQuery query,
        IPronoteClient client,
        CancellationToken cancellationToken)
    {
        if (query.Week is { } week)
        {
            return new[] { week };
        }

        if (query.Date is { } date)
        {
            return new[] { await client.GetWeekNumberAsync(date, cancellationToken).ConfigureAwait(false) };
        }

        IReadOnlyList<int> weeksWithCourses = await client
            .GetWeeksWithCoursesAsync(query.Formation, query.Section, cancellationToken)
            .ConfigureAwait(false);

        if (query.Start is not null || query.End is not null)
        {
            // Start alone: from Start to the last available week.
            // End alone: from the first available week to End.
            int startWeek = query.Start is { } start
                ? await client.GetWeekNumberAsync(start, cancellationToken).ConfigureAwait(false)
                : 1;
            int endWeek = query.End is { } end
                ? await client.GetWeekNumberAsync(end, cancellationToken).ConfigureAwait(false)
                : int.MaxValue;
            return weeksWithCourses.Where(w => w >= startWeek && w <= endWeek).OrderBy(w => w).ToArray();
        }

        return weeksWithCourses.OrderBy(w => w).ToArray();
    }
}
