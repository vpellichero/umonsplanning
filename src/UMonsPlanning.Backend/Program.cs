using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using UMonsPlanning.Backend.Catalog;
using UMonsPlanning.Backend.Contracts;
using UMonsPlanning.Backend.Endpoints;
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

builder.Services.AddValidatorsFromAssemblyContaining<ScheduleIcsQueryValidator>();

builder.Services.AddOutputCache(options =>
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5))));

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

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle("UMONS – Horaires de cours"));

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

app.UseRateLimiter();
app.UseOutputCache();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
   .WithName("Health")
   .WithSummary("Availability probe.");

app.MapCatalogEndpoints();
app.MapScheduleEndpoints();

// Serves the Angular app for every route it owns client-side (e.g. /aide) accessed directly ;
// only reached when no API/Scalar/OpenAPI endpoint above already matched. The regex excludes
// "api/..." explicitly: without it, a request with the wrong HTTP verb (e.g. HEAD on /api/health,
// as a monitoring probe would send) falls through to this route-agnostic fallback and gets a
// misleading 200 with the SPA's HTML instead of an honest 404.
app.MapFallbackToFile("{*path:regex(^(?!api/).*$):nonfile}", "index.html");

app.Run();

// Makes the Program type visible to WebApplicationFactory<Program> for integration tests.
public partial class Program;
