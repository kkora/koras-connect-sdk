using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
namespace Koras.Connect.Adp;

/// <summary>Opt-in, read-only connection diagnostics for one ADP Workforce Now connection. Runs three stages in
/// order and stops at the first failure: local configuration/certificate validation (no network), a single OAuth
/// token acquisition, and exactly one caller-selected, minimal, authorized GET. This connector never hardcodes a
/// connectivity endpoint and never issues a mutating request at any stage.</summary>
public sealed class AdpConnector : IConnector
{
    private readonly ICredentialProvider<ClientCredentialsOptions> _credentials;
    private readonly AdpWorkersOptions? _workers;
    private readonly X509Certificate2 _certificate;
    private readonly IAccessTokenSource _tokenSource;
    private readonly Func<CancellationToken, Task>? _connectivityProbe;
    private readonly TimeProvider _clock;

    public ConnectorDescriptor Descriptor { get; }

    public AdpConnector(ConnectorDescriptor descriptor, ICredentialProvider<ClientCredentialsOptions> credentials, X509Certificate2 certificate,
        IAccessTokenSource tokenSource, AdpWorkersOptions? workers = null, Func<CancellationToken, Task>? connectivityProbe = null,
        TimeProvider? clock = null)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        _workers = workers;
        _connectivityProbe = connectivityProbe;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Convenience constructor for a fixed, non-rotating credential value.</summary>
    public AdpConnector(ConnectorDescriptor descriptor, ClientCredentialsOptions credentials, X509Certificate2 certificate,
        IAccessTokenSource tokenSource, AdpWorkersOptions? workers = null, Func<CancellationToken, Task>? connectivityProbe = null,
        TimeProvider? clock = null)
        : this(descriptor, new StaticCredentialProvider<ClientCredentialsOptions>(credentials), certificate, tokenSource, workers, connectivityProbe, clock) { }

    /// <summary>Builds the connector descriptor: the catalog of documented ADP operations and their current
    /// entitlement evidence (every operation defaults to <see cref="EntitlementState.Unknown"/> unless
    /// <paramref name="entitlements"/> is supplied).</summary>
    public static ConnectorDescriptor CreateDescriptor(AdpEntitlementTracker? entitlements = null) => new(
        Provider: "ADP",
        Product: "Workforce Now",
        ApiVersion: "v2",
        SupportedOperations: AdpOperationCatalog.Contracts.Select(c => c.Operation.ToString()).ToArray(),
        Entitlements: entitlements?.ToDescriptorEntitlements() ?? AdpEntitlementTracker.Unknown());

    public async ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ConnectionDiagnostic>();

        // Stage 1: Configuration. Local validation only; never touches the network, so an expired or missing-key
        // certificate (or invalid credential/options shape) is reported before anything is sent.
        if (!await TryValidateConfigurationAsync(cancellationToken))
        {
            results.Add(new(DiagnosticStage.Configuration, DiagnosticOutcome.Failed, "configuration_invalid", null, _clock.GetUtcNow(),
                "Correct the local certificate or client credentials configuration before retrying."));
            return results;
        }
        results.Add(new(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "configuration_valid", null, _clock.GetUtcNow()));

        // Stage 2: Authentication. A single token acquisition against the configured OAuth endpoint.
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _tokenSource.GetTokenAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            var code = ex switch
            {
                AuthenticationException => "authentication_failed",
                AuthorizationException => "authorization_denied",
                _ => "authentication_unreachable"
            };
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Failed, code, CorrelationIdOf(ex), _clock.GetUtcNow(),
                "Verify the client credentials, certificate trust and OAuth endpoint reachability."));
            return results;
        }
        results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authentication_succeeded", null, _clock.GetUtcNow()));

        // Stage 3: Connectivity. Exactly one caller-selected, minimal, authorized GET. No endpoint is hardcoded
        // here: callers inject the single read they are already entitled to run (for example a metadata read).
        if (_connectivityProbe is null)
        {
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Skipped, "connectivity_probe_not_configured", null, _clock.GetUtcNow(),
                "Configure a connectivity probe (a single authorized GET) to verify reachability beyond authentication."));
            return results;
        }
        try
        {
            await _connectivityProbe(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Failed, "connectivity_probe_failed", CorrelationIdOf(ex), _clock.GetUtcNow(),
                "Confirm network reachability and that the probed operation is entitled."));
            return results;
        }
        results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Passed, "connectivity_probe_succeeded", null, _clock.GetUtcNow()));
        return results;
    }

    private async Task<bool> TryValidateConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var credentials = await _credentials.GetAsync(cancellationToken);
            credentials.Validate();
            _workers?.Validate();
            if (!_certificate.HasPrivateKey) throw new ArgumentException("Certificate requires a private key.");
            var now = _clock.GetUtcNow();
            if (now < new DateTimeOffset(_certificate.NotBefore) || now > new DateTimeOffset(_certificate.NotAfter))
                throw new ArgumentException("Certificate is not currently valid.");
        }
        catch (ArgumentException) { return false; }
        catch (Koras.Connect.Abstractions.OptionsValidationException) { return false; }
        return true;
    }

    private static string? CorrelationIdOf(Exception ex) => ex is IntegrationException integration ? integration.Details.CorrelationId : null;
}
