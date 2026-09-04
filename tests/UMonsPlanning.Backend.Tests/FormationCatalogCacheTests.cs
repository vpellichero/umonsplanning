using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UMonsPlanning.Backend.Catalog;
using UMonsPlanning.Pronote;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Tests;

public class FormationCatalogCacheTests : IDisposable
{
    private readonly string _cacheDirectory =
        Path.Combine(Path.GetTempPath(), "umons-planning-tests", Guid.NewGuid().ToString("N"));

    private readonly MutableTimeProvider _timeProvider =
        new(new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetFormationsAsync_SecondCallSameMonth_UsesCacheWithoutRefetching()
    {
        List<ResourceDto> formations = [new ResourceDto("bab3", ".BAB3")];
        var client = new Mock<IPronoteClient>(MockBehavior.Strict);
        client.Setup(c => c.GetFormationsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(formations);

        FormationCatalogCache cache = CreateCache(client.Object);

        IReadOnlyList<ResourceDto> first = await cache.GetFormationsAsync(CancellationToken.None);
        IReadOnlyList<ResourceDto> second = await cache.GetFormationsAsync(CancellationToken.None);

        first.Should().BeEquivalentTo(formations);
        second.Should().BeEquivalentTo(formations);
        client.Verify(c => c.GetFormationsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFormationsAsync_NextCalendarMonth_RefetchesFromPronote()
    {
        var client = new Mock<IPronoteClient>(MockBehavior.Strict);
        client.SetupSequence(c => c.GetFormationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ResourceDto("bab3", "September")])
            .ReturnsAsync([new ResourceDto("bab3", "October")]);

        FormationCatalogCache cache = CreateCache(client.Object);

        IReadOnlyList<ResourceDto> september = await cache.GetFormationsAsync(CancellationToken.None);
        _timeProvider.Advance(TimeSpan.FromDays(31));
        IReadOnlyList<ResourceDto> october = await cache.GetFormationsAsync(CancellationToken.None);

        september.Should().ContainSingle(r => r.Label == "September");
        october.Should().ContainSingle(r => r.Label == "October");
        client.Verify(c => c.GetFormationsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetSectionsAsync_UsesIndependentCachePerFormation()
    {
        var client = new Mock<IPronoteClient>(MockBehavior.Strict);
        client.Setup(c => c.GetSectionsAsync("bab3", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ResourceDto("d3", "D3")]);
        client.Setup(c => c.GetSectionsAsync("mab1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ResourceDto("g1", "G1")]);

        FormationCatalogCache cache = CreateCache(client.Object);

        IReadOnlyList<ResourceDto> bab3 = await cache.GetSectionsAsync("bab3", CancellationToken.None);
        IReadOnlyList<ResourceDto> mab1 = await cache.GetSectionsAsync("mab1", CancellationToken.None);

        bab3.Should().ContainSingle(r => r.Label == "D3");
        mab1.Should().ContainSingle(r => r.Label == "G1");
    }

    private FormationCatalogCache CreateCache(IPronoteClient client) => new(
        client,
        Options.Create(new CatalogOptions { CacheDirectory = _cacheDirectory }),
        _timeProvider,
        NullLogger<FormationCatalogCache>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan span) => _now += span;
    }
}
