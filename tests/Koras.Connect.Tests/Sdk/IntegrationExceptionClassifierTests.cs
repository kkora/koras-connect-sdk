using System.Net;
using Koras.Connect.Exceptions;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-009: direct, provider-neutral coverage of <see cref="IntegrationExceptionClassifier"/> itself.
/// Every status code this classifier maps was already proven indirectly through ADP/SAP/Qualtrics's own
/// exception-classification tests (each re-asserting the same mapping at their own boundary), but none of them
/// is attributable to this ticket's own owning package (<c>Koras.Connect.Core</c>) in isolation. This file is
/// the single authoritative, no-provider-reference test for the classifier's contract.</summary>
public sealed class IntegrationExceptionClassifierTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(AuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(AuthorizationException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(ThrottleException))]
    [InlineData(HttpStatusCode.RequestTimeout, typeof(TransportException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(TransportException))]
    [InlineData(HttpStatusCode.BadGateway, typeof(TransportException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(TransportException))]
    [InlineData(HttpStatusCode.GatewayTimeout, typeof(TransportException))]
    [InlineData(HttpStatusCode.NotFound, typeof(ContractException))] // "unknown provider error": no explicit case, falls to the neutral default
    [InlineData((HttpStatusCode)418, typeof(ContractException))] // a status this classifier has never seen must still classify safely, not throw
    public void ClassifyMapsEveryStatusCodeToTheDocumentedExceptionType(HttpStatusCode status, Type expectedType)
    {
        var classified = IntegrationExceptionClassifier.Classify(status, attempts: 2, correlationId: "corr-1");
        Assert.IsType(expectedType, classified);
        Assert.Equal("corr-1", classified.Details.CorrelationId);
        Assert.StartsWith("http_", classified.Details.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrottleExceptionCarriesTheServerSuppliedRetryAfterWhenProvided()
    {
        var classified = (ThrottleException)IntegrationExceptionClassifier.Classify(HttpStatusCode.TooManyRequests, 1, null, TimeSpan.FromSeconds(42));
        Assert.Equal(TimeSpan.FromSeconds(42), classified.RetryAfter);
    }

    [Fact]
    public void ClassifySignatureAcceptsOnlyStatusAttemptsAndCorrelationNeverARequestOrResponseBody()
    {
        // Structural guarantee, not a runtime check: Classify's parameter list (HttpStatusCode, int, string?,
        // TimeSpan?) has no body/payload parameter at all, so a secret-bearing response body has no path into
        // the classified exception's message through this method - this is why "secret-bearing body" is exercised
        // at each provider's own exception-wrapper boundary (AdpApiException/QualtricsApiException/SuccessFactorsClient)
        // rather than here.
        var parameters = typeof(IntegrationExceptionClassifier).GetMethod(nameof(IntegrationExceptionClassifier.Classify))!.GetParameters();
        Assert.Equal(["statusCode", "attempts", "correlationId", "retryAfter"], parameters.Select(p => p.Name));
        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(string) && p.Name != "correlationId");
    }
}
