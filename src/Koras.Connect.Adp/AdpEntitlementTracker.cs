using System.Collections.Concurrent;
using Koras.Connect.Abstractions;
namespace Koras.Connect.Adp;

/// <summary>Per-connection entitlement state for every documented <see cref="AdpOperation"/>. Every operation
/// defaults to <see cref="EntitlementState.Unknown"/>; evidence is only ever recorded from an actual configuration
/// opt-in or an authorized call outcome, never inferred from local configuration alone.</summary>
public sealed class AdpEntitlementTracker
{
    private readonly ConcurrentDictionary<AdpOperation, EntitlementEvidence> _state = new(
        Enum.GetValues<AdpOperation>().Select(o => new KeyValuePair<AdpOperation, EntitlementEvidence>(o, new EntitlementEvidence(EntitlementState.Unknown))));

    /// <summary>Returns the current evidence for one operation. Every <see cref="AdpOperation"/> value has exactly
    /// one entry from construction onward.</summary>
    public EntitlementEvidence Get(AdpOperation operation)
        => _state.TryGetValue(operation, out var evidence) ? evidence : throw new ArgumentOutOfRangeException(nameof(operation));

    /// <summary>Records evidence for one operation: for example <see cref="EntitlementState.Configured"/> after a
    /// local opt-in, or <see cref="EntitlementState.Verified"/>/<see cref="EntitlementState.Denied"/> after an
    /// authorized call outcome. <paramref name="reference"/> must never carry a credential, token or record payload.</summary>
    public void Record(AdpOperation operation, EntitlementState state, string? reference = null, DateTimeOffset? recordedAt = null)
    {
        if (!_state.ContainsKey(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
        _state[operation] = new EntitlementEvidence(state, recordedAt ?? DateTimeOffset.UtcNow, reference);
    }

    /// <summary>A snapshot suitable for <see cref="ConnectorDescriptor.Entitlements"/>, keyed by operation name.</summary>
    public IReadOnlyDictionary<string, EntitlementEvidence> ToDescriptorEntitlements()
        => _state.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

    /// <summary>The default all-<see cref="EntitlementState.Unknown"/> entitlement map, for connectors constructed
    /// without a shared tracker.</summary>
    public static IReadOnlyDictionary<string, EntitlementEvidence> Unknown()
        => Enum.GetValues<AdpOperation>().ToDictionary(o => o.ToString(), _ => new EntitlementEvidence(EntitlementState.Unknown));
}
