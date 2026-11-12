using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>Parses SAP OData V2 raw date values found in <see cref="JsonElement"/> fields such as
/// <see cref="PerPerson.DateOfBirth"/>, <see cref="EmpEmployment.StartDate"/>/<see cref="EmpEmployment.EndDate"/>
/// and <see cref="EmpJob.StartDate"/>.
/// <para><b>Policy:</b> a recognized value always resolves to a UTC <see cref="DateTimeOffset"/>. An unrecognized
/// but present value throws <see cref="FormatException"/> rather than silently returning null - this SDK never
/// masks corrupt or unexpected upstream data as "unknown". Callers that want to treat a JSON null/absent field as
/// "unknown" must check for that explicitly (see <see cref="ParseUtcOrNull"/>) before parsing.</para></summary>
public static class SapDates
{
    private static readonly Regex EpochPattern = new(@"^/Date\((-?\d+)\)/$", RegexOptions.Compiled);

    /// <summary>Parses either SAP's legacy epoch-millisecond <c>/Date(ms)/</c> shape or an ISO-8601 string.
    /// Throws <see cref="FormatException"/> for any other JSON shape or unrecognized string content.</summary>
    public static DateTimeOffset ParseUtc(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw new FormatException($"Unsupported SAP date representation: expected a JSON string, found {value.ValueKind}.");
        var raw = value.GetString();
        if (string.IsNullOrWhiteSpace(raw)) throw new FormatException("SAP date value was empty.");
        var epoch = EpochPattern.Match(raw);
        if (epoch.Success)
            return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(epoch.Groups[1].Value, CultureInfo.InvariantCulture)).ToUniversalTime();
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed.ToUniversalTime();
        throw new FormatException("Unrecognized SAP date value; expected the /Date(ms)/ shape or an ISO-8601 string.");
    }

    /// <summary>Returns null only for an explicit JSON null/absent value (<paramref name="value"/> is null or its
    /// <see cref="JsonElement.ValueKind"/> is Null/Undefined); any other unparseable value still throws, matching
    /// the type-level "never silently drop garbage" policy.</summary>
    public static DateTimeOffset? ParseUtcOrNull(JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        return ParseUtc(value.Value);
    }
}
