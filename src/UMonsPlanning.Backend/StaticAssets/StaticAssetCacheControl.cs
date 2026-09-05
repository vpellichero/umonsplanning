using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace UMonsPlanning.Backend.StaticAssets;

/// <summary>
/// Sets a Cache-Control header appropriate to how a static file's own name behaves across
/// deployments: a hashed bundle name never changes meaning, an HTML page's name never changes at
/// all (so it must always be revalidated), everything else falls back to a short cache.
/// </summary>
public static partial class StaticAssetCacheControl
{
    private static readonly HashSet<string> LongLivedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".webp", ".png", ".ico", ".woff", ".woff2" };

    public static void Apply(StaticFileResponseContext context)
    {
        string fileName = context.File.Name;
        string cacheControl = fileName switch
        {
            _ when HashedBundleFileName().IsMatch(fileName) => "public, max-age=31536000, immutable",
            _ when fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) => "no-cache",
            _ when LongLivedExtensions.Contains(Path.GetExtension(fileName)) => "public, max-age=2592000",
            _ => "public, max-age=3600"
        };
        context.Context.Response.Headers.CacheControl = cacheControl;
    }

    // Angular's outputHashing: "all" names each build's bundle "<name>-<hash>.js/.css" - never
    // reused across deployments, so it can be cached forever.
    [GeneratedRegex(@"^(main|styles|chunk|polyfills)-[A-Za-z0-9]+\.(js|css)$")]
    private static partial Regex HashedBundleFileName();
}
