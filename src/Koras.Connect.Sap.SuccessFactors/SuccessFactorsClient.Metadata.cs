using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Koras.Connect.Transport;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>A best-effort CSDL entity-type projection: the entity-set/type name, declared key property names (in
/// document order) and all declared primitive property names. Navigation properties, complex types and facets are
/// not modeled.</summary>
public sealed record EntityDescriptor(string Name, IReadOnlyList<string> Keys, IReadOnlyList<string> Properties);

public sealed partial class SuccessFactorsClient
{
    /// <summary>Hard ceiling, in characters, on the raw <c>$metadata</c> document - applied before parsing (fast
    /// rejection of an oversized response) and again via <see cref="XmlReaderSettings.MaxCharactersInDocument"/>
    /// as defense in depth during parsing. SAP CSDL metadata for a single tenant is realistically well under 1 MB
    /// of text; this cap exists to fail fast on a malformed or hostile response rather than attempt to parse an
    /// unbounded document.</summary>
    public const int MaxMetadataCharacters = 8_000_000;

    private readonly SemaphoreSlim _metadataLock = new(1, 1);
    private string? _metadataHash;
    private XDocument? _metadataDocument;

    /// <summary>Fetches and parses <c>$metadata</c>. Re-fetches from the tenant on every call; reparses only when
    /// the fetched text's SHA-256 hash differs from the currently cached document, so an unchanged schema avoids
    /// repeated XML parsing. This does not avoid the network round trip.</summary>
    public Task<XDocument> GetMetadataAsync(CancellationToken cancellationToken = default) => LoadMetadataAsync(forceReparse: false, cancellationToken);

    /// <summary>Always re-fetches and unconditionally reparses, replacing any cached document regardless of
    /// whether the content hash changed. Use when a caller needs to be certain the returned document reflects the
    /// tenant's current response rather than a cached parse.</summary>
    public Task<XDocument> RefreshMetadataAsync(CancellationToken cancellationToken = default) => LoadMetadataAsync(forceReparse: true, cancellationToken);

    private async Task<XDocument> LoadMetadataAsync(bool forceReparse, CancellationToken cancellationToken)
    {
        string text;
        try { text = await _transport.ReadTextAsync(new Uri(_root, "$metadata"), cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        if (text.Length > MaxMetadataCharacters)
            throw new InvalidDataException($"Metadata document exceeds the {MaxMetadataCharacters}-character safety cap.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        await _metadataLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceReparse && _metadataDocument is not null && string.Equals(_metadataHash, hash, StringComparison.Ordinal))
                return _metadataDocument;
            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxMetadataCharacters });
            var document = XDocument.Load(reader);
            _metadataHash = hash; _metadataDocument = document;
            return document;
        }
        finally { _metadataLock.Release(); }
    }

    /// <summary>Projects EntityType/Property/Key elements out of a parsed CSDL document, matching by local element
    /// name so the exact EDM namespace version SAP returns does not matter.</summary>
    public static IReadOnlyList<EntityDescriptor> ParseEntityDescriptors(XDocument metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var descriptors = new List<EntityDescriptor>();
        foreach (var entityType in metadata.Descendants().Where(e => e.Name.LocalName == "EntityType"))
        {
            var name = entityType.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(name)) continue;
            var keys = entityType.Descendants().Where(e => e.Name.LocalName == "PropertyRef")
                .Select(e => e.Attribute("Name")?.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToArray();
            var properties = entityType.Elements().Where(e => e.Name.LocalName == "Property")
                .Select(e => e.Attribute("Name")?.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToArray();
            descriptors.Add(new EntityDescriptor(name, keys, properties));
        }
        return descriptors;
    }
}
