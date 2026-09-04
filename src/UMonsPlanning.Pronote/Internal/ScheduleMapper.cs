using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Pronote.Internal;

/// <summary>
/// Transforms the raw <c>FonctionEmploiDuTemps</c> response into a stable model.
///
/// Source format (summary):
/// <code>
/// ListeCours: [ {
///     N: "10#...",              // identifier, specific to the session
///     p: 205,                   // slot = day * PlacesParJour + slot within the day
///     d: 8,                     // duration in slots
///     co: "#CD004C",            // color
///     dom: "[2..7,9..15]",      // recurrence weeks
///     listeC: [
///        { G: 0,  C: { L: "T-ALLE-401 - Langue ALLE" } },   // subject
///        { G: 2,  C: [ { L: "..." } ] },                     // staff
///        { G: 3,  C: [ { L: "NiDeVinci.313" } ] },           // rooms
///        { G: 14, C: [ { L: "&lt;.BAB3 - ...&gt;D3" } ] },   // groups
///        { G: 5,  C: { str: "A" } },                         // status
///        { G: 7,  C: { L: "Cours" } }                        // category
///     ] } ]
/// </code>
/// </summary>
public static class ScheduleMapper
{
    private const int GenreSubject = 0;
    private const int GenreStaff = 2;
    private const int GenreRoom = 3;
    private const int GenreStatus = 5;
    private const int GenreCategory = 7;
    private const int GenreGroup = 14;

    public static IReadOnlyList<DayDto> Map(JsonElement data, GeneralParameters parameters, int week)
    {
        var courses = new List<CourseDto>();
        HashSet<string> cancelled = ReadCancelledIds(data);

        if (data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("ListeCours", out JsonElement list) &&
            list.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in list.EnumerateArray())
            {
                CourseDto? course = MapCourse(item, parameters, week, cancelled);
                if (course is not null)
                {
                    courses.Add(course);
                }
            }
        }

        return courses
            .GroupBy(c => c.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DayDto(
                g.Key,
                g.Key.DayOfWeek,
                g.OrderBy(c => c.Start).ThenBy(c => c.Subject?.Label ?? string.Empty).ToArray()))
            .ToArray();
    }

    private static CourseDto? MapCourse(
        JsonElement element,
        GeneralParameters parameters,
        int week,
        HashSet<string> cancelled)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("p", out JsonElement placeElement) ||
            placeElement.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        int place = placeElement.GetInt32();
        int length = element.TryGetProperty("d", out JsonElement d) && d.ValueKind == JsonValueKind.Number
            ? d.GetInt32()
            : 1;

        int dayIndex = place / parameters.PlacesPerDay;
        int placeInDay = place % parameters.PlacesPerDay;

        DateOnly date = parameters.WeekStart(week).AddDays(dayIndex);
        DateTime start = date.ToDateTime(TimeOnly.MinValue)
                         + parameters.DayStart
                         + parameters.PlaceDuration * placeInDay;
        DateTime end = start + parameters.PlaceDuration * length;

        string sourceId = element.TryGetProperty("N", out JsonElement n) ? n.GetString() ?? string.Empty : string.Empty;
        string? color = element.TryGetProperty("co", out JsonElement co) ? co.GetString() : null;
        string? weeks = element.TryGetProperty("dom", out JsonElement dom) ? dom.GetString() : null;

        var rooms = new List<string>();
        var groups = new List<string>();
        var teachers = new List<string>();
        var additional = new Dictionary<string, IReadOnlyList<string>>();
        SubjectDto? subject = null;
        string? status = null;
        string? category = null;

        if (element.TryGetProperty("listeC", out JsonElement contents) && contents.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement content in contents.EnumerateArray())
            {
                if (content.ValueKind != JsonValueKind.Object ||
                    !content.TryGetProperty("G", out JsonElement g) ||
                    g.ValueKind != JsonValueKind.Number ||
                    !content.TryGetProperty("C", out JsonElement c))
                {
                    continue;
                }

                int genre = g.GetInt32();
                IReadOnlyList<string> labels = ReadLabels(c);
                if (labels.Count == 0)
                {
                    continue;
                }

                switch (genre)
                {
                    case GenreSubject:
                        subject = BuildSubject(labels[0]);
                        break;
                    case GenreRoom:
                        rooms.AddRange(labels);
                        break;
                    case GenreGroup:
                        groups.AddRange(labels);
                        break;
                    case GenreStaff:
                        teachers.AddRange(labels);
                        break;
                    case GenreStatus:
                        // PRONOTE sometimes concatenates several pieces of information ("Semaine B\nA").
                        status = string.Join(" | ", labels[0]
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        break;
                    case GenreCategory:
                        category = labels[0];
                        break;
                    default:
                        additional[genre.ToString()] = labels;
                        break;
                }
            }
        }

        return new CourseDto
        {
            Key = BuildKey(date, start, subject?.Label, groups, rooms),
            SourceId = sourceId,
            Date = date,
            DayOfWeek = date.DayOfWeek,
            Start = start,
            End = end,
            DurationMinutes = (int)(end - start).TotalMinutes,
            Subject = subject,
            Rooms = rooms,
            Groups = groups,
            Teachers = teachers,
            Status = status,
            Category = category,
            Color = color,
            IsCancelled = sourceId.Length > 0 && cancelled.Contains(sourceId),
            Additional = additional,
            Placement = new CoursePlacementDto(place, length, weeks)
        };
    }

    /// <summary>
    /// PRONOTE subject labels have the form "CODE - Title".
    /// </summary>
    private static SubjectDto BuildSubject(string label)
    {
        int separator = label.IndexOf(" - ", StringComparison.Ordinal);
        return separator > 0
            ? new SubjectDto(label, label[..separator].Trim(), label[(separator + 3)..].Trim())
            : new SubjectDto(label, null, label);
    }

    private static IReadOnlyList<string> ReadLabels(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
            {
                var labels = new List<string>();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    string? label = ReadLabel(item);
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        labels.Add(label);
                    }
                }

                return labels;
            }
            case JsonValueKind.Object:
            {
                string? label = ReadLabel(element);
                return string.IsNullOrWhiteSpace(label) ? Array.Empty<string>() : new[] { label };
            }
            case JsonValueKind.String:
            {
                string? label = element.GetString();
                return string.IsNullOrWhiteSpace(label) ? Array.Empty<string>() : new[] { label };
            }
            default:
                return Array.Empty<string>();
        }
    }

    private static string? ReadLabel(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty("L", out JsonElement l) && l.ValueKind == JsonValueKind.String)
        {
            return l.GetString();
        }

        return element.TryGetProperty("str", out JsonElement str) && str.ValueKind == JsonValueKind.String
            ? str.GetString()
            : null;
    }

    private static HashSet<string> ReadCancelledIds(JsonElement data)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("ListeAnnulationsCours", out JsonElement list) ||
            list.ValueKind != JsonValueKind.Array)
        {
            return ids;
        }

        foreach (JsonElement item in list.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                ids.Add(item.GetString()!);
            }
            else if (item.ValueKind == JsonValueKind.Object &&
                     item.TryGetProperty("N", out JsonElement n) &&
                     n.ValueKind == JsonValueKind.String)
            {
                ids.Add(n.GetString()!);
            }
        }

        return ids;
    }

    /// <summary>
    /// Deterministic key: PRONOTE renumbers its identifiers every session, so we hash the
    /// course's business content instead.
    /// </summary>
    private static string BuildKey(
        DateOnly date,
        DateTime start,
        string? subject,
        IEnumerable<string> groups,
        IEnumerable<string> rooms)
    {
        string payload = string.Join('|',
            date.ToString("yyyy-MM-dd"),
            start.ToString("HH:mm"),
            subject ?? string.Empty,
            string.Join(',', groups.OrderBy(x => x, StringComparer.Ordinal)),
            string.Join(',', rooms.OrderBy(x => x, StringComparer.Ordinal)));

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
