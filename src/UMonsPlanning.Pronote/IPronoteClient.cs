using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Pronote;

/// <summary>
/// High-level facade over the PRONOTE guest space. Extracted from <see cref="PronoteClient"/>
/// (sealed) so consumers — notably the backend's <c>FormationCatalogCache</c> — can be tested
/// with a test double instead of a real PRONOTE session.
/// </summary>
public interface IPronoteClient
{
    /// <summary>List of fields of study (dropdown <c>#id_40_bandzone_0</c>).</summary>
    Task<IReadOnlyList<ResourceDto>> GetFormationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Sub-choices of a field of study (dropdown <c>#id_40_bandzone_1</c>).</summary>
    Task<IReadOnlyList<ResourceDto>> GetSectionsAsync(string formation, CancellationToken cancellationToken = default);

    /// <summary>Structured schedule of a week.</summary>
    Task<ScheduleDto> GetScheduleAsync(string formation, string? section, int week, CancellationToken cancellationToken = default);

    /// <summary>The server's academic calendar (week numbering, holidays).</summary>
    Task<CalendarDto> GetCalendarAsync(CancellationToken cancellationToken = default);

    /// <summary>Converts a date into a PRONOTE week number.</summary>
    Task<int> GetWeekNumberAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Weeks where a resource actually has courses (for the "every week" ICS export).</summary>
    Task<IReadOnlyList<int>> GetWeeksWithCoursesAsync(string formation, string? section, CancellationToken cancellationToken = default);
}
