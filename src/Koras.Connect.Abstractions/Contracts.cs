namespace Koras.Connect.Abstractions;

public sealed record ConnectionTestResult(bool Success, string? Message = null);
public sealed record ConnectorCapabilities(string Provider, IReadOnlyList<string> Operations);
public interface IIntegrationConnector
{
    string ConnectorId { get; }
    Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);
    ConnectorCapabilities GetCapabilities();
}
public sealed class IntegrationApiException : Exception
{
    public int? StatusCode { get; }
    public IntegrationApiException(string message, int? statusCode = null, Exception? inner = null) : base(message, inner) => StatusCode = statusCode;
}
