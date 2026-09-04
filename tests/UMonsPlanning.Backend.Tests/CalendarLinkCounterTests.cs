using AwesomeAssertions;
using Microsoft.Extensions.Options;
using UMonsPlanning.Backend.Stats;

namespace UMonsPlanning.Backend.Tests;

public class CalendarLinkCounterTests : IDisposable
{
    private readonly string _cacheDirectory =
        Path.Combine(Path.GetTempPath(), "umons-planning-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetCountAsync_NoFileYet_ReturnsZero()
    {
        CalendarLinkCounter counter = CreateCounter();

        long count = await counter.GetCountAsync(CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task IncrementAsync_ReturnsAndPersistsRunningTotal()
    {
        CalendarLinkCounter counter = CreateCounter();

        long first = await counter.IncrementAsync(CancellationToken.None);
        long second = await counter.IncrementAsync(CancellationToken.None);

        first.Should().Be(1);
        second.Should().Be(2);
        (await counter.GetCountAsync(CancellationToken.None)).Should().Be(2);
    }

    [Fact]
    public async Task IncrementAsync_ConcurrentCalls_NeverLosesAnIncrement()
    {
        CalendarLinkCounter counter = CreateCounter();

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => counter.IncrementAsync(CancellationToken.None)));

        (await counter.GetCountAsync(CancellationToken.None)).Should().Be(50);
    }

    private CalendarLinkCounter CreateCounter() =>
        new(Options.Create(new StatsOptions { CacheDirectory = _cacheDirectory }));

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }
}
