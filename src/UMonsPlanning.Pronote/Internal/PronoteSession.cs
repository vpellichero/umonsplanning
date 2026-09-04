using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using UMonsPlanning.Pronote.Models;
using UMonsPlanning.Pronote.Protocol;

namespace UMonsPlanning.Pronote.Internal;

/// <summary>
/// A live PRONOTE session: session number, negotiated AES IV, order counter, and resource cache
/// (the <c>N</c> identifiers are specific to the session).
///
/// Transport is a bare <see cref="HttpClient"/>: the source API only has two entry points (GET
/// landing page, POST function call), which does not justify a declarative REST client library.
/// </summary>
public sealed partial class PronoteSession : IDisposable
{
    [GeneratedRegex(@"Start\s*\(\s*\{[^}]*?""i""\s*:\s*(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SessionIdRegex();

    private static readonly MediaTypeHeaderValue JsonContentType = new("application/json");

    private readonly PronoteOptions _options;
    private readonly ILogger _logger;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;
    private readonly byte[] _iv;

    private int _order = 1;

    private PronoteSession(
        PronoteOptions options,
        ILogger logger,
        HttpClient http,
        JsonSerializerOptions json,
        int sessionId,
        byte[] iv)
    {
        _options = options;
        _logger = logger;
        _http = http;
        _json = json;
        _iv = iv;
        SessionId = sessionId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int SessionId { get; }

    public DateTimeOffset CreatedAt { get; }

    /// <summary>General grid parameters (from FonctionParametres).</summary>
    public GeneralParameters Parameters { get; private set; } = GeneralParameters.Default;

    /// <summary>Available fields of study, indexed by slug.</summary>
    public IReadOnlyDictionary<string, PronoteResourceRef> Formations { get; private set; } =
        new Dictionary<string, PronoteResourceRef>();

    public bool IsExpired(TimeSpan lifetime) => DateTimeOffset.UtcNow - CreatedAt > lifetime;

    // -----------------------------------------------------------------------
    //  Opening
    // -----------------------------------------------------------------------

    public static async Task<PronoteSession> OpenAsync(
        PronoteOptions options,
        ILogger logger,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler
        {
            CookieContainer = new System.Net.CookieContainer(),
            UseCookies = true,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        var http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = options.HttpTimeout
        };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);

        string landingPath = options.BypassBrowserCheck ? $"{options.LandingPath}?fd=1" : options.LandingPath;

        string landing;
        try
        {
            using HttpResponseMessage landingResponse = await http.GetAsync(landingPath, cancellationToken)
                .ConfigureAwait(false);
            landingResponse.EnsureSuccessStatusCode();
            landing = await landingResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            http.Dispose();
            throw;
        }

        Match match = SessionIdRegex().Match(landing);
        if (!match.Success ||
            !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sessionId))
        {
            http.Dispose();

            // PRONOTE returns a warning page when it does not recognize the User-Agent. The
            // French substring below matches the server's own (French) warning page text.
            bool browserRejected = landing.Contains("fd=1", StringComparison.OrdinalIgnoreCase) ||
                                   landing.Contains("navigateur utilis", StringComparison.OrdinalIgnoreCase);

            throw new PronoteException(browserRejected
                ? "PRONOTE served the \"unsupported browser\" page. Enable PronoteOptions.BypassBrowserCheck " +
                  "(the ?fd=1 parameter) and check the User-Agent."
                : "Could not read the session number in the PRONOTE landing page " +
                  "(expected format Start({...\"i\":N})).");
        }

        byte[] iv = PronoteCrypto.CreateSessionIv();
        var session = new PronoteSession(options, logger, http, jsonOptions, sessionId, iv);

        // 1) FonctionParametres: negotiates the AES IV and loads the grid parameters.
        PronoteResponse parameters = await session.CallAsync(
            "FonctionParametres",
            signature: null,
            data: new ParametresRequestData
            {
                TokenMode = false,
                Uuid = PronoteCrypto.SerializeIvForServer(iv),
                BrowserId = null,
                StartTab = options.StartTab
            },
            useSessionIv: false,
            cancellationToken).ConfigureAwait(false);

        session.Parameters = GeneralParameters.Parse(parameters.DataSec?.Data ?? default, options.DayStart);

        // 2) DemandeParametreUtilisateur: mandatory — this call is what grants access rights to
        //    the schedule functions.
        await session.CallAsync(
            "DemandeParametreUtilisateur",
            new PronoteSignature { Tab = string.Empty },
            data: null,
            useSessionIv: true,
            cancellationToken).ConfigureAwait(false);

        // 3) List of fields of study (dropdown #1).
        await session.LoadFormationsAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("PRONOTE session {SessionId} opened ({Count} formations).",
            sessionId, session.Formations.Count);

        return session;
    }

    private async Task LoadFormationsAsync(CancellationToken cancellationToken)
    {
        PronoteResponse response = await CallAsync(
            "FonctionRenvoyerListeDeRessource",
            new PronoteSignature { Tab = _options.ScheduleTab },
            new ListeRessourcesRequestData(),
            useSessionIv: true,
            cancellationToken).ConfigureAwait(false);

        var map = new Dictionary<string, PronoteResourceRef>(StringComparer.OrdinalIgnoreCase);

        if (response.DataSec?.Data is { ValueKind: JsonValueKind.Object } data &&
            data.TryGetProperty("ListeRessources", out JsonElement listeRessources) &&
            listeRessources.TryGetProperty("Liste", out JsonElement liste) &&
            liste.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in liste.EnumerateArray())
            {
                PronoteResourceRef? reference = ReadResourceRef(item);
                if (reference is null)
                {
                    continue;
                }

                map[Slug.From(reference.L)] = reference;
            }
        }

        Formations = map;
    }

    // -----------------------------------------------------------------------
    //  Business functions
    // -----------------------------------------------------------------------

    public PronoteResourceRef? FindFormation(string idOrLabel)
    {
        if (string.IsNullOrWhiteSpace(idOrLabel))
        {
            return null;
        }

        if (Formations.TryGetValue(idOrLabel, out PronoteResourceRef? direct))
        {
            return direct;
        }

        return Formations.TryGetValue(Slug.From(idOrLabel), out PronoteResourceRef? bySlug) ? bySlug : null;
    }

    /// <summary>Sub-choices (TD / options) of a field of study: dropdown #2.</summary>
    public async Task<IReadOnlyList<PronoteResourceRef>> GetSectionsAsync(
        PronoteResourceRef formation,
        CancellationToken cancellationToken)
    {
        PronoteResponse response = await CallAsync(
            "FonctionListeDeTDEtOptionDuDiplome",
            SignatureFor(formation),
            new ListeTdEtOptionRequestData(),
            useSessionIv: true,
            cancellationToken).ConfigureAwait(false);

        var result = new List<PronoteResourceRef>();

        if (response.DataSec?.Data is { ValueKind: JsonValueKind.Object } data &&
            data.TryGetProperty("listeRessources", out JsonElement listeRessources) &&
            listeRessources.TryGetProperty("V", out JsonElement values) &&
            values.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in values.EnumerateArray())
            {
                PronoteResourceRef? reference = ReadResourceRef(item);
                if (reference is not null)
                {
                    result.Add(reference);
                }
            }
        }

        return result;
    }

    /// <summary>Presence weeks of a resource + consultable calendar.</summary>
    public async Task<PresenceDomain> GetPresenceDomainAsync(
        PronoteResourceRef resource,
        CancellationToken cancellationToken)
    {
        PronoteResponse response = await CallAsync(
            "FonctionDomaineDePresence",
            SignatureFor(resource),
            new DomaineDePresenceRequestData(),
            useSessionIv: true,
            cancellationToken).ConfigureAwait(false);

        JsonElement data = response.DataSec?.Data ?? default;

        return new PresenceDomain(
            PronoteSet.Parse(ReadTypedValue(data, "DomainePresence")),
            PronoteSet.Parse(ReadTypedValue(data, "PeriodeConsultation")));
    }

    /// <summary>Schedule of a week (PRONOTE week number, 1 = the week of the first Monday).</summary>
    public async Task<JsonElement> GetScheduleAsync(
        PronoteResourceRef resource,
        int week,
        CancellationToken cancellationToken)
    {
        PronoteResponse response = await CallAsync(
            "FonctionEmploiDuTemps",
            SignatureFor(resource),
            new EmploiDuTempsRequestData
            {
                Domain = PronoteTypedValue.Set(8, $"[{week}]")
            },
            useSessionIv: true,
            cancellationToken).ConfigureAwait(false);

        return response.DataSec?.Data ?? default;
    }

    private PronoteSignature SignatureFor(PronoteResourceRef resource) => new()
    {
        Tab = _options.ScheduleTab,
        Search = new List<PronoteResourceRef> { resource }
    };

    // -----------------------------------------------------------------------
    //  Transport
    // -----------------------------------------------------------------------

    private async Task<PronoteResponse> CallAsync(
        string function,
        PronoteSignature? signature,
        object? data,
        bool useSessionIv,
        CancellationToken cancellationToken)
    {
        int order = _order;
        _order += 2; // the server consumes the current order and replies with order+1

        string encryptedOrder = PronoteCrypto.EncryptOrder(order, useSessionIv ? _iv : null);

        var request = new PronoteRequest
        {
            Session = SessionId,
            Order = encryptedOrder,
            Function = function,
            DataSec = new PronoteRequestPayload { Signature = signature, Data = data }
        };

        _logger.LogDebug("PRONOTE {Function} (session {Session}, order {Order}).", function, SessionId, order);

        // PRONOTE rejects a Content-Type carrying a "charset" parameter: the request is built by
        // hand rather than through StringContent's default headers.
        string json = JsonSerializer.Serialize(request, _json);
        using var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = JsonContentType;

        using HttpResponseMessage httpResponse = await _http
            .PostAsync($"appelfonction/{_options.Espace}/{SessionId}/{encryptedOrder}", content, cancellationToken)
            .ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        await using Stream body = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        PronoteResponse response = await JsonSerializer.DeserializeAsync<PronoteResponse>(body, _json, cancellationToken)
            .ConfigureAwait(false) ?? throw new PronoteException($"Empty PRONOTE response for {function}.");

        if (response.FatalError is { } fatal)
        {
            throw new PronoteException(
                $"PRONOTE rejected the {function} call: {fatal.Title} {fatal.Message}".Trim(),
                sessionExpired: true);
        }

        if (response.DataSec?.Signature is { Error: true } error)
        {
            // error.ErrorMessage is PRONOTE's own (French) error text; matched as-is below.
            string message = error.ErrorMessage ?? "unknown error";
            bool expired = message.Contains("droits", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("expir", StringComparison.OrdinalIgnoreCase);
            throw new PronoteException($"PRONOTE rejected the {function} call: {message}", expired);
        }

        return response;
    }

    private static PronoteResourceRef? ReadResourceRef(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("N", out JsonElement n) ||
            !element.TryGetProperty("L", out JsonElement l))
        {
            return null;
        }

        int genre = element.TryGetProperty("G", out JsonElement g) && g.ValueKind == JsonValueKind.Number
            ? g.GetInt32()
            : 1;

        return new PronoteResourceRef
        {
            N = n.GetString() ?? string.Empty,
            L = l.GetString() ?? string.Empty,
            G = genre
        };
    }

    private static string? ReadTypedValue(JsonElement data, string property) =>
        data.ValueKind == JsonValueKind.Object &&
        data.TryGetProperty(property, out JsonElement node) &&
        node.ValueKind == JsonValueKind.Object &&
        node.TryGetProperty("V", out JsonElement value)
            ? value.GetString()
            : null;

    public void Dispose() => _http.Dispose();
}

/// <param name="WeeksWithCourses">Weeks where the resource actually has courses.</param>
/// <param name="ConsultableWeeks">Weeks consultable on the server.</param>
public sealed record PresenceDomain(IReadOnlyList<int> WeeksWithCourses, IReadOnlyList<int> ConsultableWeeks);
