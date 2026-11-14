using Koras.Connect.Abstractions;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>One Graph permission a downstream feature declares it needs (MGR-002), distinguishing a delegated
/// permission (represented tenant-side as an <c>oauth2PermissionGrant</c>) from an application permission
/// (represented as an <c>appRoleAssignment</c>, requiring tenant-admin consent). Declaring permissions through
/// this one typed mechanism - rather than scattered ad hoc strings per feature - lets MGR-024's diagnostic probe
/// check them as a single list.</summary>
public sealed record GraphPermissionRequirement
{
    public string Permission { get; }
    public bool IsApplicationPermission { get; }
    public string Reason { get; }

    public GraphPermissionRequirement(string Permission, bool IsApplicationPermission, string Reason)
    {
        if (string.IsNullOrWhiteSpace(Permission)) throw new ArgumentException("Permission is required.", nameof(Permission));
        if (string.IsNullOrWhiteSpace(Reason)) throw new ArgumentException("Reason is required.", nameof(Reason));
        this.Permission = Permission;
        this.IsApplicationPermission = IsApplicationPermission;
        this.Reason = Reason;
    }
}

/// <summary>Declares the Graph permissions a connection needs before any downstream feature assumes access it
/// doesn't have (MGR-002). Validation only checks the declaration is well-formed and non-empty when
/// <paramref name="RequiredPermissions"/> is supplied - it does not call Graph; MGR-024 owns the live
/// entitlement check. Per-tenant permission-request-count limits (observed, approximate figures of roughly
/// 400/155/300/30 depending on app registration type) are a vendor-side configuration constraint a caller may
/// hit when requesting many scopes, not something this SDK can work around, and are therefore documented here
/// rather than locally enforced against an unconfirmed exact number.</summary>
public sealed record GraphConnectionOptions(string TenantId, string ClientId, IReadOnlyList<GraphPermissionRequirement> RequiredPermissions) : IValidatableOptions
{
    public void Validate()
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(TenantId)) failures.Add($"{nameof(TenantId)}: is required.");
        if (string.IsNullOrWhiteSpace(ClientId)) failures.Add($"{nameof(ClientId)}: is required.");
        if (RequiredPermissions is null || RequiredPermissions.Count == 0) failures.Add($"{nameof(RequiredPermissions)}: at least one required permission must be declared.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(GraphConnectionOptions), failures);
    }
}
