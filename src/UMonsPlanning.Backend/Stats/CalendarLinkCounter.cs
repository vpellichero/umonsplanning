using System.Text.Json;
using Microsoft.Extensions.Options;

namespace UMonsPlanning.Backend.Stats;

/// <summary>
/// File-based counter of "a calendar link was generated" events (see docs/adr/0012). Same pattern
/// as <see cref="Catalog.FormationCatalogCache"/> — the target hosting has no database — but
/// simpler: a single number, incremented under a lock and persisted with an atomic write.
/// </summary>
public sealed class CalendarLinkCounter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CalendarLinkCounter(IOptions<StatsOptions> options)
    {
        Directory.CreateDirectory(options.Value.CacheDirectory);
        _path = Path.Combine(options.Value.CacheDirectory, "calendar-links.json");
    }

    /// <summary>Increments the counter and returns its new value.</summary>
    public async Task<long> IncrementAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            long count = await ReadAsync(cancellationToken).ConfigureAwait(false) + 1;
            await WriteAsync(count, cancellationToken).ConfigureAwait(false);
            return count;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<long> GetCountAsync(CancellationToken cancellationToken) => ReadAsync(cancellationToken);

    private async Task<long> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return 0;
        }

        try
        {
            await using FileStream stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<long>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // Corrupted file (write interrupted by a pool recycle): treat it as missing.
            return 0;
        }
    }

    private async Task WriteAsync(long count, CancellationToken cancellationToken)
    {
        string tempPath = $"{_path}.{Guid.NewGuid():N}.tmp";

        await using (FileStream stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, count, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, _path, overwrite: true);
    }
}
