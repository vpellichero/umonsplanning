using System.ComponentModel.DataAnnotations;

namespace UMonsPlanning.Backend.Catalog;

/// <summary>
/// Configuration for the file cache of fields-of-study lists (dropdowns).
/// </summary>
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>
    /// Storage folder for the cache files, relative to the application's content root.
    /// Must be writable by the application pool (shared hosting included).
    /// </summary>
    [Required]
    public string CacheDirectory { get; set; } = "App_Data/catalog-cache";

    /// <summary>Time zone used to determine the cache's "current month".</summary>
    [Required]
    public string TimeZoneId { get; set; } = "Europe/Brussels";
}
