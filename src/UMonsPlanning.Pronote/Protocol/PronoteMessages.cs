using System.Text.Json;
using System.Text.Json.Serialization;

namespace UMonsPlanning.Pronote.Protocol;

// ---------------------------------------------------------------------------
//  Envelope of the /appelfonction/{espace}/{session}/{no} protocol
// ---------------------------------------------------------------------------

public sealed class PronoteRequest
{
    [JsonPropertyName("session")] public int Session { get; set; }
    [JsonPropertyName("no")] public string Order { get; set; } = string.Empty;
    [JsonPropertyName("id")] public string Function { get; set; } = string.Empty;
    [JsonPropertyName("dataSec")] public PronoteRequestPayload DataSec { get; set; } = new();
}

public sealed class PronoteRequestPayload
{
    [JsonPropertyName("Signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PronoteSignature? Signature { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

public sealed class PronoteSignature
{
    [JsonPropertyName("Onglet")] public string Tab { get; set; } = string.Empty;

    [JsonPropertyName("listeRecherche")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PronoteResourceRef>? Search { get; set; }
}

public sealed class PronoteResourceRef
{
    [JsonPropertyName("N")] public string N { get; set; } = string.Empty;
    [JsonPropertyName("G")] public int G { get; set; }
    [JsonPropertyName("L")] public string L { get; set; } = string.Empty;
}

public sealed class PronoteResponse
{
    [JsonPropertyName("id")] public string? Function { get; set; }
    [JsonPropertyName("session")] public int Session { get; set; }
    [JsonPropertyName("no")] public string? Order { get; set; }
    [JsonPropertyName("dataSec")] public PronoteResponsePayload? DataSec { get; set; }

    /// <summary>Session-level error ("La page a expiré !" — "The page has expired!").</summary>
    [JsonPropertyName("Erreur")] public PronoteFatalError? FatalError { get; set; }
}

public sealed class PronoteResponsePayload
{
    [JsonPropertyName("data")] public JsonElement Data { get; set; }
    [JsonPropertyName("nom")] public string? Name { get; set; }
    [JsonPropertyName("Signature")] public PronoteResponseSignature? Signature { get; set; }
}

public sealed class PronoteResponseSignature
{
    [JsonPropertyName("Erreur")] public bool Error { get; set; }
    [JsonPropertyName("MessageErreur")] public string? ErrorMessage { get; set; }
}

public sealed class PronoteFatalError
{
    [JsonPropertyName("G")] public int G { get; set; }
    [JsonPropertyName("Titre")] public string? Title { get; set; }
    [JsonPropertyName("Message")] public string? Message { get; set; }
}

// ---------------------------------------------------------------------------
//  "data" payloads of the functions used
// ---------------------------------------------------------------------------

/// <summary>Typed PRONOTE value: <c>{"_T":8,"V":"[3]"}</c>.</summary>
public sealed class PronoteTypedValue
{
    [JsonPropertyName("_T")] public int T { get; set; }
    [JsonPropertyName("V")] public string V { get; set; } = string.Empty;

    public static PronoteTypedValue Set(int type, string value) => new() { T = type, V = value };
}

public sealed class ParametresRequestData
{
    [JsonPropertyName("modeJeton")] public bool TokenMode { get; set; }
    [JsonPropertyName("Uuid")] public string Uuid { get; set; } = string.Empty;
    [JsonPropertyName("identifiantNav")] public string? BrowserId { get; set; }
    [JsonPropertyName("ongletDemarrage")] public string StartTab { get; set; } = string.Empty;
}

public sealed class ListeRessourcesRequestData
{
    [JsonPropertyName("GenreRessource")] public int ResourceKind { get; set; } = 1;
    [JsonPropertyName("GenreRecherche")] public int SearchKind { get; set; } = 1;
    [JsonPropertyName("AvecPublicationForcee")] public bool ForcedPublication { get; set; }
    [JsonPropertyName("NomRessource")] public string Name { get; set; } = "*";
    [JsonPropertyName("PourEmail")] public bool ForEmail { get; set; }
    [JsonPropertyName("PourRessource")] public bool ForResource { get; set; }
    [JsonPropertyName("filtresRessource")] public object[] Filters { get; set; } = Array.Empty<object>();
}

public sealed class ListeTdEtOptionRequestData
{
    [JsonPropertyName("ModeRecherche")] public int SearchMode { get; set; } = 1;
    [JsonPropertyName("sansListeNominative")] public bool WithoutNominativeList { get; set; }
}

public sealed class DomaineDePresenceRequestData
{
    [JsonPropertyName("FiltreRessources")] public PronoteTypedValue ResourceFilter { get; set; } = PronoteTypedValue.Set(26, "[0,6..7]");
    [JsonPropertyName("AvecCalendrier")] public bool WithCalendar { get; set; }
}

public sealed class EmploiDuTempsRequestData
{
    [JsonPropertyName("GenrePeriodeEDT")] public int PeriodKind { get; set; } = 2;
    [JsonPropertyName("GenreAffichageEDT")] public int DisplayKind { get; set; }
    [JsonPropertyName("FiltreRessources")] public PronoteTypedValue ResourceFilter { get; set; } = PronoteTypedValue.Set(26, "[0,6..7]");
    [JsonPropertyName("AvecIndisponibilites")] public bool WithUnavailability { get; set; } = true;
    [JsonPropertyName("AvecDomaineCours")] public bool WithCourseDomain { get; set; } = true;
    [JsonPropertyName("AvecDomainePere")] public bool WithParentDomain { get; set; }
    [JsonPropertyName("filterPlagesHoraires")] public bool FilterTimeSlots { get; set; }
    [JsonPropertyName("ignorerCoursAnnules")] public bool IgnoreCancelled { get; set; }
    [JsonPropertyName("avecInfosAppel")] public bool WithAttendance { get; set; }
    [JsonPropertyName("Domaine")] public PronoteTypedValue Domain { get; set; } = PronoteTypedValue.Set(8, "[1]");
}
