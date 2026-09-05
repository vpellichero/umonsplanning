using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Scalar.AspNetCore;
using UMonsPlanning.Backend.Catalog;
using UMonsPlanning.Backend.Contracts;
using UMonsPlanning.Backend.Endpoints;
using UMonsPlanning.Backend.Stats;
using UMonsPlanning.Backend.StaticAssets;
using UMonsPlanning.Pronote;
using UMonsPlanning.Pronote.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddPronoteClient();
builder.Services.AddOptions<PronoteOptions>()
    .Bind(builder.Configuration.GetSection(PronoteOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<FormationCatalogCache>();
builder.Services.AddOptions<CatalogOptions>()
    .Bind(builder.Configuration.GetSection(CatalogOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<CalendarLinkCounter>();
builder.Services.AddOptions<StatsOptions>()
    .Bind(builder.Configuration.GetSection(StatsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddValidatorsFromAssemblyContaining<ScheduleIcsQueryValidator>();

builder.Services.AddOutputCache(options =>
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5))));

builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

builder.Services.AddResponseCompression(options =>
{
    // Safe here: no authentication, no session, no secret ever reflected in a response (§12) - the
    // usual BREACH-attack rationale for leaving HTTPS compression off doesn't apply.
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/manifest+json"]);
});

// No CORS policy: the frontend is served by this same process (same origin in every environment,
// including local dev through the Angular proxy), so no cross-origin caller is expected.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Protects the backend itself from being flooded, regardless of which endpoint is targeted.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Stricter cap on the endpoints that call PRONOTE live (not file-cached), on top of the global
    // limiter above — protects the shared PRONOTE session/order counter from an abusive client.
    options.AddPolicy(ScheduleEndpoints.PronoteRateLimitPolicyName, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Not an API meant for third-party consumption (§12) - the OpenAPI document and its Scalar UI
    // stay available locally, and in the repository for anyone who wants to read them, but are not
    // served on the deployed test/production hosts.
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("UMONS – Horaires de cours"));
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    Exception? error = feature?.Error;

    // Only expected, well-understood exceptions get their message exposed to the client: they
    // carry no server-internal detail. Anything else (an unhandled bug) must stay generic here —
    // its message can leak internals such as local file paths — and is logged server-side instead.
    (int status, string title, string? detail) = error switch
    {
        PronoteException { SessionExpired: false } ex => (StatusCodes.Status404NotFound, "PRONOTE resource not found", ex.Message),
        PronoteException ex => (StatusCodes.Status502BadGateway, "PRONOTE rejected the request", ex.Message),
        ArgumentOutOfRangeException ex => (StatusCodes.Status400BadRequest, "Invalid parameter", ex.Message),
        _ => (StatusCodes.Status500InternalServerError, "Internal error", null)
    };

    if (status == StatusCodes.Status500InternalServerError && error is not null)
    {
        ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(error, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
    }

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = detail
    });
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    await next().ConfigureAwait(false);
});

// Must run before any middleware that writes a response body, so it can wrap that body in a
// compressing stream - covers both the static assets below and the API's JSON responses.
app.UseResponseCompression();

app.UseRateLimiter();
app.UseOutputCache();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = StaticAssetContentTypes.Provider,
    OnPrepareResponse = StaticAssetCacheControl.Apply
});

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
   .WithName("Health")
   .WithSummary("Availability probe.");

app.MapCatalogEndpoints();
app.MapScheduleEndpoints();
app.MapStatsEndpoints();

// Serves every prerendered Angular route directly from its own wwwroot/<route>/index.html (e.g.
// /aide) when accessed directly, and returns a real HTTP 404 - the styled 404 page's HTML, but
// with the actual status code - for anything else. Deliberately checks the physical file instead
// of relying on UseDefaultFiles' directory-matching for a path with no trailing slash, so the
// behavior for "/aide" and "/aide/" is identical and doesn't depend on unstated middleware
// semantics. Paths under "api/" never get the HTML 404 page: an unmatched API path (or, as
// before, a monitoring probe sending HEAD on a known API path) gets a bare 404 instead.
// The "nonfile" constraint keeps this fallback from ever matching a request whose last segment
// looks like a real file (has a dot, e.g. "main-XXX.js", "robots.txt"): ASP.NET Core's endpoint
// routing marks the request as matched to this fallback as soon as the pattern matches, and
// UseStaticFiles/UseDefaultFiles above defer to whatever endpoint routing already selected instead
// of serving the physical file themselves - without this constraint, every real asset request
// would be swallowed by this handler instead of being served by the static file middleware.
app.MapFallback("{*path:nonfile}", async (HttpContext context) =>
{
    string path = context.Request.Path.Value?.Trim('/') ?? string.Empty;
    if (path.StartsWith("api/", StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    IWebHostEnvironment environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    string requestedFile = Path.Combine(environment.WebRootPath, path, "index.html");
    string fileToServe = requestedFile;
    if (!File.Exists(requestedFile))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        fileToServe = Path.Combine(environment.WebRootPath, "404", "index.html");
    }

    // Same "no-cache" policy as the .html branch of StaticAssetCacheControl, applied here too since
    // these pages are served straight from this handler rather than through UseStaticFiles.
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(fileToServe);
});

app.Run();

// Makes the Program type visible to WebApplicationFactory<Program> for integration tests.
public partial class Program;
