using UMonsPlanning.Backend.Catalog;
using UMonsPlanning.Pronote.Models;

namespace UMonsPlanning.Backend.Endpoints;

/// <summary>Fields-of-study lists for the frontend's two dropdowns.</summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/formations", async (FormationCatalogCache catalog, CancellationToken ct) =>
                Results.Ok(await catalog.GetFormationsAsync(ct)))
           .WithName("GetFormations")
           .WithSummary("Available fields of study (first dropdown).")
           .WithDescription("List cached on disk, refreshed at most once per calendar month.")
           .Produces<IReadOnlyList<ResourceDto>>();

        app.MapGet("/api/formations/{formation}/sections", async (
                string formation,
                FormationCatalogCache catalog,
                CancellationToken ct) =>
                Results.Ok(await catalog.GetSectionsAsync(formation, ct)))
           .WithName("GetSections")
           .WithSummary("Sub-choices (second dropdown) of a field of study.")
           .WithDescription("List cached on disk, refreshed at most once per calendar month.")
           .Produces<IReadOnlyList<ResourceDto>>();

        return app;
    }
}
