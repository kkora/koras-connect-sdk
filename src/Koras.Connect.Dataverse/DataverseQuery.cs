namespace Koras.Connect.Dataverse;

/// <summary>Dataverse Web API query options (DVT-005). Only the documented supported standard query options are
/// exposed - <c>$skip</c>, <c>$search</c> and <c>$format</c> are explicitly unsupported by Dataverse [D6] and are
/// intentionally absent from this type rather than accepted and rejected at call time: there is no property through
/// which a caller could even attempt to request them (Dataverse paginates via <c>$skiptoken</c>/
/// <c>odata.maxpagesize</c> instead - see <see cref="DataverseClient.EnumerateAsync"/>). Parameter aliases
/// (<c>@p1=...</c>) in <see cref="Filter"/>/<see cref="OrderBy"/> are a documented Dataverse capability but are not
/// implemented by this connector; supply a fully literal, already-escaped filter/orderby expression instead.</summary>
public sealed record DataverseQuery(string? Select = null, string? Filter = null, string? OrderBy = null,
    string? Expand = null, string? Apply = null, int? Top = null, bool Count = false)
{
    public override string ToString() => "DataverseQuery { values=[REDACTED] }";
}
