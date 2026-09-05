using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UMonsPlanning.Backend.Catalog;
using UMonsPlanning.Pronote;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Tests;

/// <summary>
/// Verifies the browser-facing Cache-Control header on the catalog endpoints (LOT 1: lets the
/// browser skip the round-trip entirely while fresh, matching how rarely the underlying list
/// actually changes). Replaces <see cref="FormationCatalogCache"/> with an instance backed by a
/// mocked <see cref="IPronoteClient"/> so this never calls the real PRONOTE server.
/// </summary>
public sealed class CatalogCacheHeadersTests : IDisposable
{
    private readonly string _cacheDirectory =
        Path.Combine(Path.GetTempPath(), "umons-planning-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetFormations_SetsCacheControlForBrowserCaching()
    {
        var client = new Mock<IPronoteClient>(MockBehavior.Strict);
        client.Setup(c => c.GetFormationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ResourceDto("bab3", ".BAB3")]);

        using WebApplicationFactory<Program> factory = CreateFactory(client.Object);
        using HttpClient httpClient = factory.CreateClient();

        HttpResponseMessage response = await httpClient.GetAsync("/api/formations", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Be("public, max-age=3600, stale-while-revalidate=86400");
    }

    private WebApplicationFactory<Program> CreateFactory(IPronoteClient pronoteClient) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<FormationCatalogCache>();
            services.AddSingleton(new FormationCatalogCache(
                pronoteClient,
                Options.Create(new CatalogOptions { CacheDirectory = _cacheDirectory, TimeZoneId = "Europe/Brussels" }),
                TimeProvider.System,
                NullLogger<FormationCatalogCache>.Instance));
        }));

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }
}
