using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UMonsPlanning.Backend.Tests;

/// <summary>
/// Verifies the deterministic static-file fallback in <c>Program.cs</c>: a prerendered route is
/// served from its own <c>wwwroot/&lt;route&gt;/index.html</c> regardless of trailing slash, an
/// unknown route gets a real HTTP 404 (the styled not-found page, not a disguised 200), and an
/// unknown API path never gets the HTML 404 page.
/// </summary>
public sealed class StaticFallbackTests : IDisposable
{
    private readonly string _webRoot =
        Path.Combine(Path.GetTempPath(), "umons-planning-tests", Guid.NewGuid().ToString("N"));

    public StaticFallbackTests()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot, "aide"));
        Directory.CreateDirectory(Path.Combine(_webRoot, "404"));
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<html><body>home</body></html>");
        File.WriteAllText(Path.Combine(_webRoot, "aide", "index.html"), "<html><body>aide</body></html>");
        File.WriteAllText(Path.Combine(_webRoot, "404", "index.html"), "<html><body>not found</body></html>");
        File.WriteAllText(Path.Combine(_webRoot, "robots.txt"), "User-agent: *");
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseWebRoot(_webRoot));

    [Fact]
    public async Task GetPrerenderedRoute_WithoutTrailingSlash_ReturnsItsOwnHtmlWithOk()
    {
        using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await client.GetAsync("/aide", ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("aide");
    }

    [Fact]
    public async Task GetPrerenderedRoute_WithTrailingSlash_ReturnsItsOwnHtmlWithOk()
    {
        using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await client.GetAsync("/aide/", ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("aide");
    }

    [Fact]
    public async Task GetUnknownRoute_Returns404WithNotFoundPageHtml()
    {
        using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await client.GetAsync("/page-inexistante-test-404", ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().Contain("not found");
    }

    [Fact]
    public async Task GetRealStaticFile_IsServedByStaticFileMiddleware_NotSwallowedByTheFallback()
    {
        // Regression test: a fallback pattern that matches every path (no "nonfile" constraint)
        // makes ASP.NET Core's endpoint routing select the fallback for every request, which makes
        // UseStaticFiles defer to it instead of serving the physical file - breaking every JS/CSS/
        // static asset on the site.
        using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await client.GetAsync("/robots.txt", ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("User-agent: *");
    }

    [Fact]
    public async Task GetUnknownApiRoute_Returns404WithoutHtmlBody()
    {
        using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        CancellationToken ct = TestContext.Current.CancellationToken;

        HttpResponseMessage response = await client.GetAsync("/api/does-not-exist", ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }
}
