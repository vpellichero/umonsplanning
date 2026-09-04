namespace UMonsPlanning.Backend.Contracts;

/// <summary>Generation mode for the .ics file produced by <c>GET /api/schedule.ics</c>.</summary>
public enum IcsLayout
{
    /// <summary>One iCalendar event per course (default).</summary>
    PerCourse,

    /// <summary>A single iCalendar event per day; course details are listed in its description.</summary>
    PerDay,
}
