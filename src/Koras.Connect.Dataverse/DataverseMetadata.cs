using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>A best-effort CSDL entity-type projection for a Dataverse table (DVT-009): the entity logical name,
/// declared key property names (in document order) and all declared primitive property names. Navigation
/// properties, complex types and facets are not modeled, matching <c>SuccessFactorsClient.ParseEntityDescriptors</c>'s
/// scope for a different OData dialect.</summary>
public sealed record DataverseEntityDescriptor(string LogicalName, IReadOnlyList<string> Keys, IReadOnlyList<string> Properties);

public sealed partial class DataverseClient
{
    /// <summary>Hard ceiling, in characters, on the raw <c>$metadata</c> document - applied before parsing (fast
    /// rejection of an oversized response) and again via <see cref="XmlReaderSettings.MaxCharactersInDocument"/>
    /// as defense in depth during parsing. Matches <c>SuccessFactorsClient</c>'s identical cap; real-world
    /// Dataverse metadata document size for a heavily customized tenant is unverified against this cap this pass.</summary>
    public const int MaxMetadataCharacters = 8_000_000;

    private readonly SemaphoreSlim _metadataLock = new(1, 1);
    private string? _metadataHash;
    private XDocument? _metadataDocument;

    /// <summary>Fetches and parses <c>$metadata</c> (DVT-009). Re-fetches from the environment on every call;
    /// reparses only when the fetched text's SHA-256 hash differs from the currently cached document.</summary>
    public Task<XDocument> GetMetadataAsync(CancellationToken cancellationToken = default) => LoadMetadataAsync(forceReparse: false, cancellationToken);

    /// <summary>Always re-fetches and unconditionally reparses, replacing any cached document regardless of
    /// whether the content hash changed.</summary>
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
    /// name so the exact EDM namespace version Dataverse returns does not matter.</summary>
    public static IReadOnlyList<DataverseEntityDescriptor> ParseEntityDescriptors(XDocument metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var descriptors = new List<DataverseEntityDescriptor>();
        foreach (var entityType in metadata.Descendants().Where(e => e.Name.LocalName == "EntityType"))
        {
            var name = entityType.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(name)) continue;
            var keys = entityType.Descendants().Where(e => e.Name.LocalName == "PropertyRef")
                .Select(e => e.Attribute("Name")?.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToArray();
            var properties = entityType.Elements().Where(e => e.Name.LocalName == "Property")
                .Select(e => e.Attribute("Name")?.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToArray();
            descriptors.Add(new DataverseEntityDescriptor(name, keys, properties));
        }
        return descriptors;
    }

    /// <summary>Enumerates the <c>EntityDefinitions</c> metadata-query entity set (DVT-009), for example filtered
    /// by <c>query.Filter = "ChangeTrackingEnabled eq true"</c>. This is a distinct, PascalCase-named metadata
    /// entity set, not a regular business table - it is not passed through <see cref="ValidateEntitySet"/>'s
    /// lowercase-table-name rule.</summary>
    public IAsyncEnumerable<System.Text.Json.JsonElement> EnumerateEntityDefinitionsAsync(DataverseQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateMetadataEntityAsync("EntityDefinitions", query, cancellationToken);
}
