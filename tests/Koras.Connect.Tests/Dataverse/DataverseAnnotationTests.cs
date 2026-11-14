using System.Text.Json;
using Koras.Connect.Dataverse;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-017: attachments and notes - <see cref="DataverseAnnotation"/> round-trips the documented
/// <c>annotation</c> fields without data loss and preserves unknown fields.</summary>
public sealed class DataverseAnnotationTests
{
    [Fact]
    public void RoundTripsDocumentedFieldsWithoutDataLoss()
    {
        const string json = """{"filename":"report.pdf","mimetype":"application/pdf","documentbody":"SGVsbG8=","isdocument":true,"subject":"keepme"}""";
        var annotation = JsonSerializer.Deserialize<DataverseAnnotation>(json)!;
        Assert.Equal("report.pdf", annotation.FileName);
        Assert.Equal("application/pdf", annotation.MimeType);
        Assert.Equal("SGVsbG8=", annotation.DocumentBodyBase64);
        Assert.True(annotation.IsDocument);
        Assert.True(annotation.AdditionalData!.ContainsKey("subject"));

        var reserialized = JsonSerializer.Serialize(annotation);
        var roundTripped = JsonSerializer.Deserialize<DataverseAnnotation>(reserialized)!;
        Assert.Equal(annotation.FileName, roundTripped.FileName);
        Assert.Equal(annotation.DocumentBodyBase64, roundTripped.DocumentBodyBase64);
        Assert.True(roundTripped.AdditionalData!.ContainsKey("subject"));
    }

    [Fact]
    public void DocumentBodyIsNeverAutoDecoded()
    {
        var annotation = JsonSerializer.Deserialize<DataverseAnnotation>("""{"documentbody":"SGVsbG8="}""")!;
        Assert.Equal("SGVsbG8=", annotation.DocumentBodyBase64);
        Assert.Equal("Hello", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(annotation.DocumentBodyBase64!)));
    }
}
