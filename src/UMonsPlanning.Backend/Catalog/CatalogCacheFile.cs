using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Catalog;

/// <summary>
/// Persisted content of a dropdown cache file.
/// </summary>
/// <param name="CacheMonth">Generation month, formatted <c>yyyy-MM</c> (time zone <see cref="CatalogOptions.TimeZoneId"/>).</param>
/// <param name="GeneratedAtUtc">Generation timestamp, for diagnostic purposes.</param>
/// <param name="Items">Cached items.</param>
public sealed record CatalogCacheFile(string CacheMonth, DateTimeOffset GeneratedAtUtc, IReadOnlyList<ResourceDto> Items);
