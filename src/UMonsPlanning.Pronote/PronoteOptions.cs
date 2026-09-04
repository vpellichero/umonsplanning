using System.ComponentModel.DataAnnotations;

namespace UMonsPlanning.Pronote;

/// <summary>
/// Configuration for the PRONOTE client (the "Horaires de cours" guest space).
/// </summary>
public sealed class PronoteOptions
{
    public const string SectionName = "Pronote";

    /// <summary>Root of the PRONOTE server, e.g. https://hplanning2026.umons.ac.be </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://hplanning2026.umons.ac.be";

    /// <summary>Path of the landing page that creates the session (guest space).</summary>
    [Required]
    public string LandingPath { get; set; } = "invite";

    /// <summary>
    /// Adds <c>?fd=1</c> to the landing page to bypass the server-side browser filtering done by
    /// PRONOTE. Leave <c>true</c> for any non-browser client.
    /// </summary>
    public bool BypassBrowserCheck { get; set; } = true;

    /// <summary>PRONOTE space kind. 2 = guest space.</summary>
    [Range(1, int.MaxValue)]
    public int Espace { get; set; } = 2;

    /// <summary>Startup tab announced to the server.</summary>
    [Required]
    public string StartTab { get; set; } = "DIPLOME.EDT";

    /// <summary>Tab used for schedule calls ("grid" view).</summary>
    [Required]
    public string ScheduleTab { get; set; } = "DIPLOME.EDT.EDT_GRILLE";

    /// <summary>
    /// Start time of the timetable grid (slot 0 of the day).
    /// PRONOTE does not publish this value in the guest space: it is calibrated to 08:00
    /// (verified against the UMONS server's real display).
    /// </summary>
    public TimeSpan DayStart { get; set; } = TimeSpan.FromHours(8);

    /// <summary>Lifetime of a PRONOTE session before automatic re-creation.</summary>
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>HTTP timeout.</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>User-Agent sent to the server.</summary>
    [Required]
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36";
}
