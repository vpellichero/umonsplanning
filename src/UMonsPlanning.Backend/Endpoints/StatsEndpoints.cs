using UMonsPlanning.Backend.Stats;

namespace UMonsPlanning.Backend.Endpoints;

/// <summary>Vanity counter of calendar links generated, shown on the home page.</summary>
public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/stats/calendar-links", async (CalendarLinkCounter counter, CancellationToken ct) =>
                Results.Ok(new CalendarLinkStatsDto(await counter.IncrementAsync(ct))))
           .WithName("RecordCalendarLinkGenerated")
           .WithSummary("Records one calendar link generation.")
           .WithDescription("""
                Called by the frontend only when the user copies the generated link — never by the
                "test" preview, and never by a calendar app refreshing an already-subscribed
                calendar (those only ever call GET /api/schedule.ics). See docs/adr/0012.
                """)
           .Produces<CalendarLinkStatsDto>();

        app.MapGet("/api/stats/calendar-links", async (CalendarLinkCounter counter, CancellationToken ct) =>
                Results.Ok(new CalendarLinkStatsDto(await counter.GetCountAsync(ct))))
           .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(30)))
           .WithName("GetCalendarLinksGenerated")
           .WithSummary("Current value of the home page's calendar-links-generated counter.")
           .WithDescription(
               "Cached 30s server-side (shorter than the app's default 5-minute output-cache " +
               "policy): a vanity counter can lag briefly behind a just-recorded generation, but " +
               "not for as long as that default would allow.")
           .Produces<CalendarLinkStatsDto>();

        return app;
    }
}
