using System.Globalization;
using System.Text.Json;

namespace UMonsPlanning.Pronote.Internal;

/// <summary>
/// Timetable grid parameters, extracted from the <c>FonctionParametres</c> response
/// (<c>dataSec.data.parametreGeneral</c>).
/// </summary>
public sealed record GeneralParameters
{
    private static readonly string[] DateFormats = { "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yyyy H:m:s", "d/M/yyyy H:m:s" };

    public static readonly GeneralParameters Default = new()
    {
        FirstMonday = new DateOnly(2026, 9, 7),
        LastDate = new DateOnly(2027, 9, 5),
        PlacesPerDay = 68,
        PlacesPerHour = 4,
        DayStart = TimeSpan.FromHours(8),
        HolidayWeeks = Array.Empty<int>(),
        SchoolName = string.Empty,
        ProductVersion = string.Empty
    };

    /// <summary>Monday of PRONOTE week #1.</summary>
    public required DateOnly FirstMonday { get; init; }

    public required DateOnly LastDate { get; init; }

    /// <summary>Number of elementary slots per day (68 at UMONS).</summary>
    public required int PlacesPerDay { get; init; }

    /// <summary>Number of slots per hour (4 => 15-minute slots).</summary>
    public required int PlacesPerHour { get; init; }

    /// <summary>Time corresponding to slot 0 of a day.</summary>
    public required TimeSpan DayStart { get; init; }

    public required IReadOnlyList<int> HolidayWeeks { get; init; }

    public required string SchoolName { get; init; }

    public required string ProductVersion { get; init; }

    /// <summary>Duration of an elementary slot.</summary>
    public TimeSpan PlaceDuration => TimeSpan.FromMinutes(60d / PlacesPerHour);

    /// <summary>Total number of weeks in the academic year.</summary>
    public int WeekCount => Math.Max(1, (int)Math.Ceiling((LastDate.ToDateTime(TimeOnly.MinValue) -
                                                          FirstMonday.ToDateTime(TimeOnly.MinValue)).TotalDays / 7d));

    public DateOnly WeekStart(int week) => FirstMonday.AddDays((week - 1) * 7);

    /// <summary>
    /// Closest PRONOTE week number for a given date, always between 1 and <see cref="WeekCount"/>.
    /// A date before the academic year's first Monday returns week 1 ; a date after
    /// <see cref="LastDate"/> returns the last week — rather than an out-of-range number (0 or
    /// beyond <see cref="WeekCount"/>) that would fail any subsequent call requiring a valid week
    /// number (e.g. "test my calendar" on a day before the term starts).
    /// </summary>
    public int WeekNumberFor(DateOnly date)
    {
        int days = date.DayNumber - FirstMonday.DayNumber;
        int week = days < 0 ? 1 : days / 7 + 1;
        return Math.Clamp(week, 1, WeekCount);
    }

    public static GeneralParameters Parse(JsonElement data, TimeSpan dayStart)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("parametreGeneral", out JsonElement general))
        {
            return Default with { DayStart = dayStart };
        }

        return new GeneralParameters
        {
            FirstMonday = ReadDate(general, "PremierLundi") ?? Default.FirstMonday,
            LastDate = ReadDate(general, "DerniereDate") ?? Default.LastDate,
            PlacesPerDay = ReadInt(general, "PlacesParJour") ?? Default.PlacesPerDay,
            PlacesPerHour = ReadInt(general, "PlacesParHeure") ?? Default.PlacesPerHour,
            DayStart = dayStart,
            HolidayWeeks = PronoteSet.Parse(ReadTyped(general, "SemainesFeriees")),
            ProductVersion = ReadTyped(general, "Version") ?? string.Empty,
            SchoolName = ReadSchoolName(data) ?? string.Empty
        };
    }

    private static string? ReadSchoolName(JsonElement data) =>
        data.TryGetProperty("parametres", out JsonElement parameters) &&
        parameters.ValueKind == JsonValueKind.Object &&
        parameters.TryGetProperty("Divers", out JsonElement divers) &&
        divers.ValueKind == JsonValueKind.Array &&
        divers.GetArrayLength() > 0 &&
        divers[0].TryGetProperty("NomEtablissement", out JsonElement name)
            ? name.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static string? ReadTyped(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement node) &&
        node.ValueKind == JsonValueKind.Object &&
        node.TryGetProperty("V", out JsonElement value)
            ? value.GetString()
            : null;

    private static DateOnly? ReadDate(JsonElement element, string property)
    {
        string? raw = ReadTyped(element, property);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;
    }
}
