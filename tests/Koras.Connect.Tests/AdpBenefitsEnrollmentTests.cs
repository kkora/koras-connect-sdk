using System.Net;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-017: the remaining unit-test-requirement edge cases for spending-account enrollment reads beyond
/// what <see cref="AdpContractTests.BenefitsRequireEffectiveDateAndPreserveDocumentedFilter"/> and
/// <see cref="AdpTypedModelsTests"/>'s round-trip test already cover &#8212; missing identifier, blank-body 400,
/// denied grant for this specific operation, masked/unmasked field tolerance, and overlapping-period/dependent
/// data preserved raw.</summary>
public sealed class AdpBenefitsEnrollmentTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../escape")]
    public async Task MissingOrUnsafeIdentifierIsRejectedBeforeAnyRequestIsSent(string associateOid)
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("Should not call the network.")));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.SpendingAccountEnrollments]);
        await Assert.ThrowsAsync<ArgumentException>(() => client.ReadSpendingAccountEnrollmentsAsync(associateOid, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public async Task BlankBody400ClassifiesAsContractExceptionWithoutLeakingTheBody()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.BadRequest) { Content = new StringContent("") }));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.SpendingAccountEnrollments]);
        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.ReadSpendingAccountEnrollmentsAsync("w1", new DateOnly(2026, 1, 1)));
        Assert.Equal("ContractException", ex.Category);
    }

    [Fact]
    public async Task DeniedEntitlementForSpendingAccountEnrollmentsShortCircuitsBeforeAnyNetworkCall()
    {
        var tracker = new AdpEntitlementTracker();
        tracker.Record(AdpOperation.SpendingAccountEnrollments, EntitlementState.Denied, "synthetic-denial");
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("Should not call the network.")));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.SpendingAccountEnrollments], tracker);
        await Assert.ThrowsAsync<Koras.Connect.Exceptions.AuthorizationException>(() => client.ReadSpendingAccountEnrollmentsAsync("w1", new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void MaskedAndUnmaskedCoverageFieldsBothSurviveRoundTripUnmodified()
    {
        var response = JsonSerializer.Deserialize<SpendingAccountEnrollmentsResponse>("""
            {"spendingAccountEnrollments":[
                {"itemID":"E1","accountBasedCoverage":{"planName":"FSA","accountNumber":"***-**-1234"}},
                {"itemID":"E2","accountBasedCoverage":{"planName":"HSA","accountNumber":"ACME-00482913"}}
            ]}
            """)!;
        Assert.Equal(2, response.Enrollments.Count);
        Assert.Equal("***-**-1234", response.Enrollments[0].AccountBasedCoverage!.Value.GetProperty("accountNumber").GetString());
        Assert.Equal("ACME-00482913", response.Enrollments[1].AccountBasedCoverage!.Value.GetProperty("accountNumber").GetString());
    }

    [Fact]
    public void OverlappingCoveragePeriodsAndActiveInactiveDependentsAreAllPreservedRaw()
    {
        var response = JsonSerializer.Deserialize<SpendingAccountEnrollmentsResponse>("""
            {"spendingAccountEnrollments":[{"itemID":"E1","accountBasedCoverage":{
                "coveragePeriods":[
                    {"validFrom":"2026-01-01","validTo":"2026-12-31"},
                    {"validFrom":"2026-06-01","validTo":"2027-05-31"}
                ],
                "dependents":[
                    {"name":"Active Child","isActive":true},
                    {"name":"Former Dependent","isActive":false}
                ]
            }}]}
            """)!;
        var coverage = response.Enrollments[0].AccountBasedCoverage!.Value;
        Assert.Equal(2, coverage.GetProperty("coveragePeriods").GetArrayLength());
        var dependents = coverage.GetProperty("dependents").EnumerateArray().ToArray();
        Assert.Equal(2, dependents.Length);
        Assert.True(dependents[0].GetProperty("isActive").GetBoolean());
        Assert.False(dependents[1].GetProperty("isActive").GetBoolean());
    }
}
