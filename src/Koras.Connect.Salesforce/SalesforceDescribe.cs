using System.Globalization;
using System.Net;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>Per-field describe metadata (SFD-007).</summary>
public sealed record SalesforceFieldDescriptor(string Name, string Type, bool Createable, bool Updateable, bool Nillable,
    bool Unique, bool ExternalId, IReadOnlyList<string>? ReferenceTo, string? RelationshipName);

/// <summary>Object (or global-describe entry) describe metadata (SFD-007). <see cref="Fields"/> is empty for a
/// global-describe entry - only a per-object describe includes field metadata.</summary>
public sealed record SalesforceObjectDescriptor(string Name, string Label, bool Createable, bool Updateable, bool Deletable,
    bool Queryable, string? KeyPrefix, IReadOnlyList<SalesforceFieldDescriptor> Fields);

public sealed partial class SalesforceClient
{
    /// <summary>Object describe: <c>GET .../sobjects/{sObject}/describe/</c> (SFD-007). When
    /// <paramref name="ifModifiedSince"/> is supplied and the org reports the schema unchanged, returns <c>null</c>
    /// rather than throwing - a <c>304</c> is a documented "unchanged" outcome, not a parse failure.</summary>
    public async Task<SalesforceObjectDescriptor?> DescribeObjectAsync(string sObject, DateTimeOffset? ifModifiedSince = null, CancellationToken cancellationToken = default)
    {
        ValidateSObject(sObject);
        var address = new Uri(_root, $"sobjects/{sObject}/describe/");
        var headers = ifModifiedSince is not null
            ? new Dictionary<string, string> { ["If-Modified-Since"] = ifModifiedSince.Value.ToString("R", CultureInfo.InvariantCulture) }
            : null;
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, headers, cancellationToken); }
        catch (IntegrationRequestException ex) when (ex.ResponseStatus == HttpStatusCode.NotModified) { return null; }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return ParseObjectDescriptor(document.RootElement); }
    }

    /// <summary>Global describe: <c>GET .../sobjects/</c> → every accessible object's summary metadata, with no
    /// field-level detail (SFD-007).</summary>
    public async Task<IReadOnlyList<SalesforceObjectDescriptor>> DescribeGlobalAsync(CancellationToken cancellationToken = default)
    {
        var address = new Uri(_root, "sobjects/");
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("sobjects", out var sobjects) || sobjects.ValueKind != JsonValueKind.Array)
                throw new ContractException("Global describe response did not contain a sobjects array.", new ErrorDetails("describe_contract"));
            var descriptors = new List<SalesforceObjectDescriptor>();
            foreach (var item in sobjects.EnumerateArray()) descriptors.Add(ParseGlobalDescribeEntry(item));
            return descriptors;
        }
    }

    private static SalesforceObjectDescriptor ParseGlobalDescribeEntry(JsonElement item)
        => new(RequireString(item, "name"), RequireString(item, "label"), RequireBool(item, "createable"), RequireBool(item, "updateable"),
            RequireBool(item, "deletable"), RequireBool(item, "queryable"), OptionalString(item, "keyPrefix"), []);

    private static SalesforceObjectDescriptor ParseObjectDescriptor(JsonElement root)
    {
        var fields = new List<SalesforceFieldDescriptor>();
        if (root.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
            foreach (var field in fieldsElement.EnumerateArray()) fields.Add(ParseFieldDescriptor(field));
        return new(RequireString(root, "name"), RequireString(root, "label"), RequireBool(root, "createable"), RequireBool(root, "updateable"),
            RequireBool(root, "deletable"), RequireBool(root, "queryable"), OptionalString(root, "keyPrefix"), fields);
    }

    private static SalesforceFieldDescriptor ParseFieldDescriptor(JsonElement field)
    {
        IReadOnlyList<string>? referenceTo = null;
        if (field.TryGetProperty("referenceTo", out var referenceToElement) && referenceToElement.ValueKind == JsonValueKind.Array)
            referenceTo = referenceToElement.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToArray();
        return new(RequireString(field, "name"), RequireString(field, "type"), RequireBool(field, "createable"), RequireBool(field, "updateable"),
            RequireBool(field, "nillable"), RequireBool(field, "unique"), RequireBool(field, "externalId"), referenceTo, OptionalString(field, "relationshipName"));
    }

    private static bool RequireBool(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ContractException($"Describe response missing required field '{property}'.", new ErrorDetails("describe_contract"));
        return value.GetBoolean();
    }

    private static string? OptionalString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
