using Microsoft.AspNetCore.StaticFiles;

namespace UMonsPlanning.Backend.StaticAssets;

/// <summary>Content-type mapping for static files, extending the framework defaults.</summary>
public static class StaticAssetContentTypes
{
    public static readonly FileExtensionContentTypeProvider Provider = CreateProvider();

    private static FileExtensionContentTypeProvider CreateProvider()
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".webmanifest"] = "application/manifest+json";
        return provider;
    }
}
