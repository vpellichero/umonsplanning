using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOutputCache(options =>
    options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5))));

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle("UMONS – Horaires de cours"));

app.UseCors();
app.UseOutputCache();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    Exception? error = feature?.Error;

    (int status, string title) = error switch
    {
        PronoteException { SessionExpired: false } => (StatusCodes.Status404NotFound, "PRONOTE resource not found"),
        PronoteException => (StatusCodes.Status502BadGateway, "PRONOTE rejected the request"),
        ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, "Invalid parameter"),
        _ => (StatusCodes.Status500InternalServerError, "Internal error")
    };

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = error?.Message
    });
}));

app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }))
   .WithName("Health")
   .WithSummary("Availability probe.");

app.MapCatalogEndpoints();
app.MapScheduleEndpoints();

app.Run();

// Makes the Program type visible to WebApplicationFactory<Program> for integration tests.
public partial class Program;
