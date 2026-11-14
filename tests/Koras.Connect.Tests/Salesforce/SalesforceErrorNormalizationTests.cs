using System.Net;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-019: Error normalization - documented <c>errorCode</c> values map to the SDK's typed exception
/// hierarchy, with an unrecognized code falling back to <see cref="ContractException"/>.</summary>
public sealed class SalesforceErrorNormalizationTests
{
    [Fact]
    public void InvalidSessionIdClassifiesAsAuthentication()
    {
        var ex = SalesforceApiException.ClassifyErrorCode(HttpStatusCode.Unauthorized, "INVALID_SESSION_ID");
        Assert.IsType<AuthenticationException>(ex.InnerException);
        Assert.Equal("INVALID_SESSION_ID", ex.Details.Code);
    }

    [Fact]
    public void InsufficientAccessClassifiesAsAuthorization()
    {
        var ex = SalesforceApiException.ClassifyErrorCode(HttpStatusCode.Forbidden, "INSUFFICIENT_ACCESS");
        Assert.IsType<AuthorizationException>(ex.InnerException);
    }

    [Fact]
    public void RequestLimitExceededClassifiesAsThrottleAndCarriesNoAssumedRetryAfter()
    {
        var ex = SalesforceApiException.ClassifyErrorCode(HttpStatusCode.TooManyRequests, "REQUEST_LIMIT_EXCEEDED");
        var throttle = Assert.IsType<ThrottleException>(ex.InnerException);
        Assert.Null(throttle.RetryAfter);
    }

    [Theory]
    [InlineData("REQUIRED_FIELD_MISSING")]
    [InlineData("INVALID_FIELD")]
    [InlineData("MALFORMED_ID")]
    [InlineData("DUPLICATE_VALUE")]
    [InlineData("FIELD_INTEGRITY_EXCEPTION")]
    [InlineData("RECORD_NOT_FOUND")]
    [InlineData("ENTITY_IS_DELETED")]
    [InlineData("ENTITY_IS_LOCKED")]
    [InlineData("STORAGE_LIMIT_EXCEEDED")]
    [InlineData("INVALID_CROSS_REFERENCE_KEY")]
    public void EveryOtherDocumentedErrorCodeClassifiesAsContract(string errorCode)
    {
        var ex = SalesforceApiException.ClassifyErrorCode(HttpStatusCode.BadRequest, errorCode);
        Assert.IsType<ContractException>(ex.InnerException);
        Assert.Equal(errorCode, ex.Details.Code);
    }

    [Fact]
    public void AnUnrecognizedErrorCodeFallsBackToContractRatherThanThrowingAnUnrelatedError()
    {
        var ex = SalesforceApiException.ClassifyErrorCode(HttpStatusCode.BadRequest, "SOME_FUTURE_UNKNOWN_CODE");
        Assert.IsType<ContractException>(ex.InnerException);
        Assert.Equal("SOME_FUTURE_UNKNOWN_CODE", ex.Details.Code);
    }

    [Fact]
    public void TheRawMessageIsNeverAttachedToTheThrownException()
    {
        var ex = SalesforceApiException.ClassifyErrorCode(HttpStatusCode.BadRequest, "INVALID_FIELD", message: "Field 'SensitiveValue' is not writable: 42");
        Assert.DoesNotContain("SensitiveValue", ex.Message);
        Assert.DoesNotContain("42", ex.Message);
    }
}
