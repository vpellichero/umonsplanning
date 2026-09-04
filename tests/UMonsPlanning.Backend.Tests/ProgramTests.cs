using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UMonsPlanning.Backend.Tests;

/// <summary>
/// Verifies that the host actually starts (valid dependency graph, <c>ValidateOnStart</c> options
/// satisfied) and routes correctly, without touching PRONOTE (tests-git.md forbids any network
/// dependency in tests).
/// </summary>
public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScheduleIcs_InvalidQuery_ReturnsValidationProblem()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/schedule.ics?formation=bab3&week=3&date=2026-09-21",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
