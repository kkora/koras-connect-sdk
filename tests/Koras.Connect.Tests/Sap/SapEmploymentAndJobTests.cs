using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapEmploymentAndJobTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body,Encoding.UTF8,"application/json") };
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");
    private static SuccessFactorsClient Client(HttpClient http) => new(new(http,new Token()),new(SapRoot));

    [Fact] public async Task EnumerateEmploymentPreservesConcurrentRecordsOpenEndedDatesAndUnknownFields()
    {
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("""
        {"d":{"results":[
            {"userId":"synthetic-1","personIdExternal":"p1","startDate":"/Date(1700000000000)/","endDate":"/Date(1750000000000)/","customFlag":true},
            {"userId":"synthetic-1","personIdExternal":"p1","startDate":"2025-06-01T00:00:00Z","custom":"concurrent"}
        ]}}
        """))));
        var c=Client(http);
        var employments=new List<EmpEmployment>();
        await foreach(var e in c.EnumerateEmploymentAsync()) employments.Add(e);
        Assert.Equal(2,employments.Count);
        Assert.All(employments,e=>Assert.Equal("synthetic-1",e.UserId));
        Assert.Equal(JsonValueKind.String,employments[0].StartDate!.Value.ValueKind);
        Assert.Equal(JsonValueKind.String,employments[0].EndDate!.Value.ValueKind);
        Assert.Null(employments[1].EndDate); // open-ended: absent endDate deserializes to a null JsonElement?
        Assert.True(employments[0].AdditionalData!["customFlag"].GetBoolean());
        Assert.Equal("concurrent",employments[1].AdditionalData!["custom"].GetString());
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),SapDates.ParseUtc(employments[0].StartDate!.Value));
        Assert.Equal(DateTimeOffset.Parse("2025-06-01T00:00:00Z"),SapDates.ParseUtc(employments[1].StartDate!.Value));
    }

    [Fact] public async Task EnumerateEmploymentAsyncForwardsFilterAndAsOfDateInTheQueryString()
    {
        using var http=new HttpClient(new Handler((r,ct)=>
        {
            Assert.Equal("/odata/v2/EmpEmployment",r.RequestUri!.AbsolutePath);
            Assert.Contains("asOfDate=2026-01-01",r.RequestUri.Query);
            Assert.Contains("$filter=userId%20eq%20",r.RequestUri.Query);
            Assert.Contains("synthetic-1",r.RequestUri.Query);
            return Task.FromResult(Json("""{"d":{"results":[]}}"""));
        }));
        var c=Client(http);
        await foreach(var e in c.EnumerateEmploymentAsync(new(Filter:"userId eq 'synthetic-1'",AsOfDate:new(2026,1,1)))) { }
    }

    [Fact] public async Task EnumerateEmploymentAsyncTreatsAnRbpFilteredEmptyResultTheSameAsLegitimatelyNoRecords()
    {
        // SAP-008 "invisible records": SAP's documented collection-query behavior returns a plain 200 with an
        // empty array when the caller lacks permission to see matching records - indistinguishable, by design,
        // from a query that legitimately matches nothing. The SDK must not treat this as an error or special-case
        // it; this test pins that an empty page is simply an empty enumeration, nothing more.
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("""{"d":{"results":[]}}"""))));
        var c=Client(http);
        var employments=new List<EmpEmployment>();
        await foreach(var e in c.EnumerateEmploymentAsync(new(Filter:"userId eq 'denied-user'"))) employments.Add(e);
        Assert.Empty(employments);
    }

    [Fact] public async Task EnumerateJobsAcceptsSequenceNumberAsStringOrNumberAndPreservesUnknownFields()
    {
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("""
        {"d":{"results":[
            {"userId":"synthetic-1","seqNumber":3,"managerId":"synthetic-mgr","jobTitle":"Engineer","custom":"a"},
            {"userId":"synthetic-1","seqNumber":"4","managerId":"synthetic-mgr","jobTitle":"Senior Engineer","custom":"b"}
        ]}}
        """))));
        var c=Client(http);
        var jobs=new List<EmpJob>();
        await foreach(var j in c.EnumerateJobsAsync()) jobs.Add(j);
        Assert.Equal(2,jobs.Count);
        Assert.Equal(3L,jobs[0].SequenceNumber);
        Assert.Equal(4L,jobs[1].SequenceNumber); // string-encoded sequence number still parses via AllowReadingFromString
        Assert.Equal("a",jobs[0].AdditionalData!["custom"].GetString());
        Assert.Equal("b",jobs[1].AdditionalData!["custom"].GetString());
    }

    [Fact] public async Task EnumerateJobsAsyncForwardsFromAndToDateRangeInTheQueryString()
    {
        using var http=new HttpClient(new Handler((r,ct)=>
        {
            Assert.Equal("/odata/v2/EmpJob",r.RequestUri!.AbsolutePath);
            Assert.Contains("fromDate=2026-01-01",r.RequestUri.Query);
            Assert.Contains("toDate=2026-12-31",r.RequestUri.Query);
            return Task.FromResult(Json("""{"d":{"results":[]}}"""));
        }));
        var c=Client(http);
        await foreach(var j in c.EnumerateJobsAsync(new(FromDate:new(2026,1,1),ToDate:new(2026,12,31)))) { }
    }

    [Fact] public async Task SameDayJobsWithDifferentSequenceNumbersPreserveDistinctOrderingIdentity()
    {
        // SAP-009 "same-day sequences": two EmpJob records sharing a startDate are only distinguishable by
        // seqNumber - both must survive enumeration as distinct entries (ordering itself is covered by
        // SapManagerResolutionTests.IsMoreCurrent via ManagerAssignment; this test pins the raw EmpJob read).
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("""
        {"d":{"results":[
            {"userId":"synthetic-1","seqNumber":1,"startDate":"2026-06-01T00:00:00Z","jobTitle":"Engineer I"},
            {"userId":"synthetic-1","seqNumber":2,"startDate":"2026-06-01T00:00:00Z","jobTitle":"Engineer II"}
        ]}}
        """))));
        var c=Client(http);
        var jobs=new List<EmpJob>();
        await foreach(var j in c.EnumerateJobsAsync()) jobs.Add(j);
        Assert.Equal(2,jobs.Count);
        Assert.Equal(jobs[0].StartDate!.Value.GetString(),jobs[1].StartDate!.Value.GetString());
        Assert.NotEqual(jobs[0].SequenceNumber,jobs[1].SequenceNumber);
        Assert.Equal("Engineer II",jobs.Single(j=>j.SequenceNumber==2).JobTitle);
    }
}
