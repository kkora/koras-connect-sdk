namespace Koras.Connect.Abstractions;

/// <summary>Runtime entitlement state for a connector operation. Never inferred from local configuration alone.</summary>
public enum EntitlementState
{
    /// <summary>No attempt has been made to confirm this operation against the remote tenant.</summary>
    Unknown,
    /// <summary>Local configuration/opt-in enables an attempt; the remote grant has not been confirmed.</summary>
    Configured,
    /// <summary>A prior authorized call confirmed the remote tenant grants this operation.</summary>
    Verified,
    /// <summary>A prior authorized call confirmed the remote tenant denies this operation.</summary>
    Denied
}

/// <summary>Evidence backing an <see cref="EntitlementState"/>. Carries no secrets or payload data.</summary>
public sealed record EntitlementEvidence(EntitlementState State, DateTimeOffset? RecordedAt = null, string? Reference = null);

/// <summary>Identifies a connector instance and its statically known, entitlement-aware operation surface.</summary>
public sealed record ConnectorDescriptor(
    string Provider,
    string Product,
    string ApiVersion,
    IReadOnlyList<string> SupportedOperations,
    IReadOnlyDictionary<string, EntitlementEvidence> Entitlements);

/// <summary>Stage of a <see cref="IConnector.DiagnoseAsync"/> probe. <see cref="Permissions"/> is an optional
/// stage beyond the base three - a connector surfaces it only when it has its own permission/consent
/// inspection surface to probe (for example Microsoft Graph's MGR-024); connectors without one simply never
/// emit it.</summary>
public enum DiagnosticStage
{
    Configuration,
    Authentication,
    Connectivity,
    Permissions
}

/// <summary>Outcome of a single diagnostic stage.</summary>
public enum DiagnosticOutcome
{
    Passed,
    Failed,
    Skipped
}

/// <summary>Sanitized diagnostic result. Never contains secrets, tokens or record payloads.</summary>
public sealed record ConnectionDiagnostic(
    DiagnosticStage Stage,
    DiagnosticOutcome Outcome,
    string SanitizedCode,
    string? CorrelationId,
    DateTimeOffset Timestamp,
    string? RecommendedNextStep = null);

/// <summary>Common identity and read-only diagnostic surface implemented by provider connectors.</summary>
public interface IConnector
{
    ConnectorDescriptor Descriptor { get; }

    /// <summary>Runs a bounded, read-only, opt-in probe. Must never issue a mutating request.</summary>
    ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default);
}
