using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UMonsPlanning.Pronote;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Catalog;

/// <summary>
/// File-based cache of PRONOTE fields-of-study lists (dropdowns #1 and #2).
///
/// The target hosting (shared IIS/Plesk) does not guarantee a long-running process: refreshing
/// is therefore not driven by a timer (<see cref="Microsoft.Extensions.Hosting.BackgroundService"/>)
/// but triggered on demand, on the first request of the month that finds the cache file stale.
/// </summary>
public sealed class FormationCatalogCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IPronoteClient _client;
    private readonly CatalogOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<FormationCatalogCache> _logger;
    private readonly SemaphoreSlim _formationsGate = new(1, 1);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sectionGates = new(StringComparer.OrdinalIgnoreCase);

    public FormationCatalogCache(
        IPronoteClient client,
        IOptions<CatalogOptions> options,
        TimeProvider timeProvider,
        ILogger<FormationCatalogCache> logger)
    {
        _client = client;
        _options = options.Value;
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        _logger = logger;

        Directory.CreateDirectory(_options.CacheDirectory);
    }

    /// <summary>Fields of study (dropdown #1), refreshed at most once per calendar month.</summary>
    public Task<IReadOnlyList<ResourceDto>> GetFormationsAsync(CancellationToken cancellationToken) =>
        GetOrRefreshAsync(
            "formations.json",
            _formationsGate,
            () => _client.GetFormationsAsync(cancellationToken),
            cancellationToken);

    /// <summary>Sub-choices (dropdown #2) of a field of study, refreshed at most once per calendar month.</summary>
    public Task<IReadOnlyList<ResourceDto>> GetSectionsAsync(string formationSlug, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = _sectionGates.GetOrAdd(formationSlug, static _ => new SemaphoreSlim(1, 1));
        string fileName = $"sections-{Uri.EscapeDataString(formationSlug)}.json";

        return GetOrRefreshAsync(
            fileName,
            gate,
            () => _client.GetSectionsAsync(formationSlug, cancellationToken),
            cancellationToken);
    }

    private async Task<IReadOnlyList<ResourceDto>> GetOrRefreshAsync(
        string fileName,
        SemaphoreSlim gate,
        Func<Task<IReadOnlyList<ResourceDto>>> fetchFresh,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(_options.CacheDirectory, fileName);
        string currentMonth = CurrentCacheMonth();

        CatalogCacheFile? cached = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (cached is not null && cached.CacheMonth == currentMonth)
        {
            return cached.Items;
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Another concurrent call may have refreshed the file while we waited for the lock.
            cached = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
            if (cached is not null && cached.CacheMonth == currentMonth)
            {
                return cached.Items;
            }

            _logger.LogInformation("Cache {File} is stale or missing, refreshing from PRONOTE.", fileName);

            IReadOnlyList<ResourceDto> fresh = await fetchFresh().ConfigureAwait(false);
            await WriteAsync(path, new CatalogCacheFile(currentMonth, _timeProvider.GetUtcNow(), fresh), cancellationToken)
                .ConfigureAwait(false);
            return fresh;
        }
        finally
        {
            gate.Release();
        }
    }

    private string CurrentCacheMonth()
    {
        DateTimeOffset nowLocal = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);
        return nowLocal.ToString("yyyy-MM");
    }

    private static async Task<CatalogCacheFile?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<CatalogCacheFile>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // Corrupted file (write interrupted by a pool recycle): treat it as missing.
            return null;
        }
    }

    private static async Task WriteAsync(string path, CatalogCacheFile content, CancellationToken cancellationToken)
    {
        string tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

        await using (FileStream stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, content, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
