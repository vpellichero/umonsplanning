using System.Text.Json.Serialization;

namespace UMonsPlanning.Pronote.Models;

/// <summary>A field of study (dropdown #1) or a sub-choice (dropdown #2).</summary>
/// <param name="Id">Stable slug computed from the label (PRONOTE identifiers change every session).</param>
/// <param name="Label">Label as displayed by PRONOTE, e.g. ".BAB3 - Traduction et interprétation".</param>
public sealed record ResourceDto(string Id, string Label);

/// <summary>A week of the academic year.</summary>
public sealed record WeekDto(
    int Number,
    DateOnly Start,
    DateOnly End,
    int IsoWeek,
    bool IsHoliday);

/// <summary>Subject of a course.</summary>
public sealed record SubjectDto(string Label, string? Code, string? Name);

/// <summary>A course placed on the grid.</summary>
public sealed record CourseDto
{
    /// <summary>Deterministic key, stable across calls (hash of the course's business content).</summary>
    public required string Key { get; init; }

    /// <summary>Raw PRONOTE identifier — only valid within the session that produced it.</summary>
    public required string SourceId { get; init; }

    public required DateOnly Date { get; init; }
    public required DayOfWeek DayOfWeek { get; init; }
    public required DateTime Start { get; init; }
    public required DateTime End { get; init; }
    public required int DurationMinutes { get; init; }

    public SubjectDto? Subject { get; init; }
    public IReadOnlyList<string> Rooms { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Groups { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Teachers { get; init; } = Array.Empty<string>();

    /// <summary>PRONOTE category ("Cours", "Examen", ...).</summary>
    public string? Category { get; init; }

    /// <summary>Status ("A", "A valider", ...).</summary>
    public string? Status { get; init; }

    public string? Color { get; init; }
    public bool IsCancelled { get; init; }

    /// <summary>Unrecognized content, indexed by the PRONOTE "genre".</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Additional { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Raw placement data, useful for debugging.</summary>
    public CoursePlacementDto Placement { get; init; } = new(0, 0, null);
}

/// <param name="Place">Slot index within the week (day * placesPerDay + slot within the day).</param>
/// <param name="Length">Duration in number of slots.</param>
/// <param name="Weeks">Recurrence weeks declared by PRONOTE, e.g. "[2..7,9..15]".</param>
public sealed record CoursePlacementDto(int Place, int Length, string? Weeks);

/// <summary>Courses of a single day.</summary>
public sealed record DayDto(DateOnly Date, DayOfWeek DayOfWeek, IReadOnlyList<CourseDto> Courses);

/// <summary>Structured API response for a week.</summary>
public sealed record ScheduleDto
{
    public required ResourceDto Formation { get; init; }
    public ResourceDto? Section { get; init; }
    public required WeekDto Week { get; init; }
    public required int CourseCount { get; init; }
    public required IReadOnlyList<DayDto> Days { get; init; }

    /// <summary>Every week where this resource has courses (as returned by PRONOTE).</summary>
    public IReadOnlyList<int> WeeksWithCourses { get; init; } = Array.Empty<int>();

    public DateTimeOffset RetrievedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>General parameters of the server's timetable grid.</summary>
public sealed record CalendarDto(
    string SchoolName,
    string ProductVersion,
    DateOnly FirstMonday,
    DateOnly LastDate,
    int PlacesPerDay,
    int PlacesPerHour,
    TimeSpan DayStart,
    IReadOnlyList<WeekDto> Weeks);

/// <summary>Business error returned by PRONOTE.</summary>
public sealed class PronoteException : Exception
{
    public PronoteException(string message, bool sessionExpired = false) : base(message)
        => SessionExpired = sessionExpired;

    /// <summary>True if the session must be recreated.</summary>
    public bool SessionExpired { get; }
}
