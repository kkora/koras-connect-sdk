using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Core;

namespace Koras.Connect.ADP;

/// <summary>Minimal, provider-configured ADP worker read client. API entitlements and response schema must be validated against the customer's ADP product.</summary>
public sealed class AdpWorkersClient(HttpClient http, Uri workersEndpoint, Func<CancellationToken, Task<string>> tokenProvider) : IIntegrationConnector
{
    public string ConnectorId => "adp";
    public ConnectorCapabilities GetCapabilities() => new("ADP", ["workers.read.raw"]);
    public async Task<JsonDocument> GetWorkersRawAsync(CancellationToken ct = default)
    {
        var token = await tokenProvider(ct);
        return await new HttpJsonClient(http).GetAsync(workersEndpoint, token, ct);
    }
    public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try { using var data = await GetWorkersRawAsync(cancellationToken); return new(true, "Worker endpoint responded."); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or IntegrationApiException or JsonException or InvalidOperationException)
        { return new(false, ex.Message); }
    }
}
