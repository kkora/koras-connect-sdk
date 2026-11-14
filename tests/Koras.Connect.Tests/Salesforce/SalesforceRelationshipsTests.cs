using Koras.Connect.Salesforce;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-015: relationships and references - the <c>TYPEOF</c> polymorphic-relationship clause builder.</summary>
public sealed class SalesforceRelationshipsTests
{
    [Fact]
    public void ComposesTheDocumentedSyntax()
    {
        var clause = new SalesforceTypeOfClause("What",
            new Dictionary<string, IReadOnlyList<string>> { ["Account"] = ["Name"], ["Opportunity"] = ["Amount", "Name"] },
            ElseFields: ["Name"]);
        Assert.Equal("TYPEOF What WHEN Account THEN Name WHEN Opportunity THEN Amount,Name ELSE Name END", clause.ToString());
    }

    [Fact]
    public void OmittingElseFieldsStillComposesCorrectly()
    {
        var clause = new SalesforceTypeOfClause("Who", new Dictionary<string, IReadOnlyList<string>> { ["Contact"] = ["Name"] });
        Assert.Equal("TYPEOF Who WHEN Contact THEN Name END", clause.ToString());
    }

    [Fact]
    public void EmptyWhenFieldsThrows()
    {
        var clause = new SalesforceTypeOfClause("What", new Dictionary<string, IReadOnlyList<string>>());
        Assert.Throws<ArgumentException>(() => clause.ToString());
    }

    [Theory]
    [InlineData("bad field")]
    [InlineData("1Field")]
    [InlineData("Field;DROP")]
    public void UnsafeFieldNamesAreRejected(string unsafeField)
    {
        var clause = new SalesforceTypeOfClause(unsafeField, new Dictionary<string, IReadOnlyList<string>> { ["Account"] = ["Name"] });
        Assert.Throws<ArgumentException>(() => clause.ToString());
    }
}
