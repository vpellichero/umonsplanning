using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMonsPlanning.Pronote.Internal;
using UMonsPlanning.Pronote.Models;
using UMonsPlanning.Pronote.Protocol;

namespace UMonsPlanning.Pronote;

/// <summary>
/// High-level client for PRONOTE's "Horaires de cours" (course schedule) guest space.
///
/// Handles opening, reusing, and automatically renewing the session (PRONOTE calls are strictly
/// sequential: the order number is a counter shared with the server).
/// </summary>
public sealed class PronoteClient : IPronoteClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly PronoteOptions _options;
    private readonly ILogger<PronoteClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private PronoteSession? _session;
    private bool _disposed;

    public PronoteClient(IOptions<PronoteOptions> options, ILogger<PronoteClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>List of fields of study (dropdown <c>#id_40_bandzone_0</c>).</summary>
    public Task<IReadOnlyList<ResourceDto>> GetFormationsAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(session => Task.FromResult<IReadOnlyList<ResourceDto>>(
            session.Formations.Values
                .Select(r => new ResourceDto(Slug.From(r.L), r.L))
                .OrderBy(r => r.Label, StringComparer.InvariantCulture)
                .ToArray()),
            cancellationToken);

    /// <summary>Sub-choices of a field of study (dropdown <c>#id_40_bandzone_1</c>).</summary>
    public Task<IReadOnlyList<ResourceDto>> GetSectionsAsync(string formation, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async session =>
        {
            PronoteResourceRef reference = ResolveFormation(session, formation);
            IReadOnlyList<PronoteResourceRef> sections = await session.GetSectionsAsync(reference, cancellationToken)
                .ConfigureAwait(false);

            return (IReadOnlyList<ResourceDto>)sections
                .Select(r => new ResourceDto(Slug.From(r.L), r.L))
                .ToArray();
        }, cancellationToken);

    /// <summary>Structured schedule of a week.</summary>
    /// <param name="formation">Slug or exact label of the field of study.</param>
    /// <param name="section">Slug or exact label of the sub-choice (optional).</param>
    /// <param name="week">PRONOTE week number (1 = the week of the academic year's first Monday).</param>
    public Task<ScheduleDto> GetScheduleAsync(
        string formation,
        string? section,
        int week,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async session =>
        {
            if (week < 1 || week > session.Parameters.WeekCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(week),
                    week,
                    $"Week must be between 1 and {session.Parameters.WeekCount}.");
            }

            PronoteResourceRef formationRef = ResolveFormation(session, formation);
            PronoteResourceRef target = formationRef;
            ResourceDto? sectionDto = null;

            if (!string.IsNullOrWhiteSpace(section))
            {
                IReadOnlyList<PronoteResourceRef> sections =
                    await session.GetSectionsAsync(formationRef, cancellationToken).ConfigureAwait(false);

                PronoteResourceRef? match = sections.FirstOrDefault(
                    s => string.Equals(s.L, section, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(Slug.From(s.L), Slug.From(section), StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    throw new PronoteException(
                        $"Sub-choice '{section}' not found for '{formationRef.L}'. " +
                        $"Available values: {string.Join(", ", sections.Select(s => s.L))}");
                }

                target = match;
                sectionDto = new ResourceDto(Slug.From(match.L), match.L);
            }

            PresenceDomain presence = await session.GetPresenceDomainAsync(target, cancellationToken)
                .ConfigureAwait(false);

            JsonElement raw = await session.GetScheduleAsync(target, week, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<DayDto> days = ScheduleMapper.Map(raw, session.Parameters, week);

            return new ScheduleDto
            {
                Formation = new ResourceDto(Slug.From(formationRef.L), formationRef.L),
                Section = sectionDto,
                Week = BuildWeek(session.Parameters, week),
                CourseCount = days.Sum(d => d.Courses.Count),
                Days = days,
                WeeksWithCourses = presence.WeeksWithCourses
            };
        }, cancellationToken);

    /// <summary>The server's academic calendar (week numbering, holidays).</summary>
    public Task<CalendarDto> GetCalendarAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(session =>
        {
            GeneralParameters p = session.Parameters;
            var weeks = Enumerable.Range(1, p.WeekCount).Select(w => BuildWeek(p, w)).ToArray();

            return Task.FromResult(new CalendarDto(
                p.SchoolName,
                p.ProductVersion,
                p.FirstMonday,
                p.LastDate,
                p.PlacesPerDay,
                p.PlacesPerHour,
                p.DayStart,
                weeks));
        }, cancellationToken);

    /// <summary>
    /// Converts a date into a PRONOTE week number, always between 1 and the academic year's week
    /// count (see <see cref="GeneralParameters.WeekNumberFor"/>).
    /// </summary>
    public Task<int> GetWeekNumberAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        ExecuteAsync(session => Task.FromResult(session.Parameters.WeekNumberFor(date)), cancellationToken);

    /// <summary>Weeks where a resource actually has courses (for the "every week" ICS export).</summary>
    public Task<IReadOnlyList<int>> GetWeeksWithCoursesAsync(
        string formation,
        string? section,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async session =>
        {
            PronoteResourceRef formationRef = ResolveFormation(session, formation);
            PronoteResourceRef target = formationRef;

            if (!string.IsNullOrWhiteSpace(section))
            {
                IReadOnlyList<PronoteResourceRef> sections =
                    await session.GetSectionsAsync(formationRef, cancellationToken).ConfigureAwait(false);

                target = sections.FirstOrDefault(
                    s => string.Equals(s.L, section, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(Slug.From(s.L), Slug.From(section), StringComparison.OrdinalIgnoreCase))
                    ?? throw new PronoteException($"Sub-choice '{section}' not found for '{formationRef.L}'.");
            }

            PresenceDomain presence = await session.GetPresenceDomainAsync(target, cancellationToken)
                .ConfigureAwait(false);
            return presence.WeeksWithCourses;
        }, cancellationToken);

    private static WeekDto BuildWeek(GeneralParameters parameters, int week)
    {
        DateOnly start = parameters.WeekStart(week);
        return new WeekDto(
            week,
            start,
            start.AddDays(6),
            ISOWeek.GetWeekOfYear(start.ToDateTime(TimeOnly.MinValue)),
            parameters.HolidayWeeks.Contains(week));
    }

    private static PronoteResourceRef ResolveFormation(PronoteSession session, string formation)
    {
        PronoteResourceRef? reference = session.FindFormation(formation);
        if (reference is null)
        {
            throw new PronoteException($"Field of study '{formation}' not found.");
        }

        return reference;
    }

    // -----------------------------------------------------------------------
    //  Session management
    // -----------------------------------------------------------------------

    private async Task<T> ExecuteAsync<T>(Func<PronoteSession, Task<T>> action, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PronoteSession session = await GetOrCreateSessionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await action(session).ConfigureAwait(false);
            }
            catch (PronoteException ex) when (ex.SessionExpired)
            {
                _logger.LogWarning(ex, "Invalid PRONOTE session, reopening and retrying.");
                DisposeSession();
                session = await GetOrCreateSessionAsync(cancellationToken).ConfigureAwait(false);
                return await action(session).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PronoteSession> GetOrCreateSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is { } current && !current.IsExpired(_options.SessionLifetime))
        {
            return current;
        }

        DisposeSession();
        _session = await PronoteSession.OpenAsync(_options, _logger, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return _session;
    }

    private void DisposeSession()
    {
        _session?.Dispose();
        _session = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeSession();
        _gate.Dispose();
    }
}
