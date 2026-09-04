namespace UMonsPlanning.Backend.Contracts;

/// <summary>
/// Parameters of <c>GET /api/schedule.ics</c>. Only one week-selection mode at a time:
/// <see cref="Week"/>, or <see cref="Date"/>, or a period (<see cref="Start"/> and/or
/// <see cref="End"/>), or none of the three (every week with courses).
/// </summary>
public sealed record ScheduleIcsQuery
{
    /// <summary>Slug or exact label returned by <c>/api/formations</c>.</summary>
    public required string Formation { get; init; }

    /// <summary>Slug or exact label returned by <c>/api/formations/{formation}/sections</c> (optional).</summary>
    public string? Section { get; init; }

    /// <summary>PRONOTE week number (1 = the week of the academic year's first Monday).</summary>
    public int? Week { get; init; }

    /// <summary>Date from which the PRONOTE week is derived.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Start of the period (inclusive). Optional: if omitted while <see cref="End"/> is provided,
    /// the period starts at the first available week.
    /// </summary>
    public DateOnly? Start { get; init; }

    /// <summary>
    /// End of the period (inclusive). Optional: if omitted while <see cref="Start"/> is provided,
    /// the period ends at the last available week.
    /// </summary>
    public DateOnly? End { get; init; }

    /// <summary>Generation mode for the .ics file. Defaults to <see cref="IcsLayout.PerCourse"/> when omitted.</summary>
    public IcsLayout? Layout { get; init; }
}
