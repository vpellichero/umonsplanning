using System.ComponentModel.DataAnnotations;

namespace UMonsPlanning.Backend.Stats;

/// <summary>
/// Configuration for the file-based counter of generated calendar links (see
/// <see cref="CalendarLinkCounter"/> and docs/adr/0012).
/// </summary>
public sealed class StatsOptions
{
    public const string SectionName = "Stats";

    /// <summary>
    /// Storage folder for the counter file, relative to the application's content root.
    /// Must be writable by the application pool (shared hosting included).
    /// </summary>
    [Required]
    public string CacheDirectory { get; set; } = "App_Data/stats";
}
