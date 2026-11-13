using System.Text.Json;
namespace Koras.Connect.Adp.Workers;

/// <summary>Builds the <see cref="AdpOperation.ChangeManager"/> event envelope from the already-modeled
/// <see cref="Identifier"/>/<see cref="ManagerRelationship"/> shapes used to read the very same "reportsTo"
/// collection (see <see cref="WorkAssignmentTypedExtensions.GetManagerRelationships"/>). This is only the inverse
/// projection of data whose shape the SDK already trusts from reads &#8212; no field is introduced here beyond
/// <c>workerID.idValue</c>, <c>reportsToWorkerName</c> and <c>reportingRelationshipCode.codeValue</c>, all already
/// modeled on <see cref="ManagerRelationship"/>. The outer <c>events[].data.eventContext</c>/<c>transform</c>
/// envelope matches the vendor events convention documented in docs/features/ADP-FEATURE-MATRIX.md ("Manager
/// change ... vendor events envelope; customer reports-to context/transform") and required generically by
/// <see cref="AdpOperationsClient.SubmitEventAsync"/>. The exact current tenant schema for this write remains
/// unverified &#8212; see ADP-013's remaining-work note; do not treat a successful local build as proof of tenant
/// acceptance.</summary>
public static class ChangeManagerCommand
{
    /// <summary>Builds the submit-ready envelope for <see cref="AdpOperationsClient.SubmitEventAsync"/>. Throws
    /// <see cref="ArgumentException"/> locally &#8212; before any HTTP call is attempted &#8212; when the subject
    /// worker's associate OID or the target manager's worker id is missing.</summary>
    public static JsonElement Build(string associateOid, ManagerRelationship manager)
    {
        if (string.IsNullOrWhiteSpace(associateOid)) throw new ArgumentException("The subject worker's associate OID is required.", nameof(associateOid));
        ArgumentNullException.ThrowIfNull(manager);
        var managerWorkerId = manager.WorkerId?.Value;
        if (string.IsNullOrWhiteSpace(managerWorkerId)) throw new ArgumentException("The target manager's workerID.idValue is required.", nameof(manager));

        var reportsToEntry = new Dictionary<string, object?> { ["workerID"] = new Dictionary<string, object?> { ["idValue"] = managerWorkerId } };
        if (!string.IsNullOrWhiteSpace(manager.ReportsToWorkerName)) reportsToEntry["reportsToWorkerName"] = manager.ReportsToWorkerName;
        if (!string.IsNullOrWhiteSpace(manager.ReportingRelationshipCode?.Code))
            reportsToEntry["reportingRelationshipCode"] = new Dictionary<string, object?> { ["codeValue"] = manager.ReportingRelationshipCode!.Code };

        var envelope = new
        {
            events = new object[]
            {
                new
                {
                    data = new
                    {
                        eventContext = new { worker = new { associateOID = associateOid } },
                        transform = new { worker = new { reportsTo = new object[] { reportsToEntry } } }
                    }
                }
            }
        };
        return JsonSerializer.SerializeToElement(envelope);
    }
}

/// <summary>Builds the <see cref="AdpOperation.ModifyDepartmentAllocations"/> event envelope from the already-
/// modeled <see cref="OrganizationDepartment"/> shape used to read <c>GET /hr/v1/organization-departments</c>. No
/// field is introduced here beyond <c>itemID</c> and <c>nameCode.codeValue</c>, both already modeled on
/// <see cref="OrganizationDepartment"/>; the target list maps onto the already-modeled
/// <see cref="WorkAssignment.OrganizationalUnits"/> ("assignedOrganizationalUnits") collection. Per
/// docs/features/KORAS-CONNECT-SDK-MASTER-FEATURES.md section 13, this is historically documented as an allocation
/// change, not a general home-department replacement; the exact current tenant schema remains unverified.</summary>
public static class ModifyDepartmentAllocationsCommand
{
    /// <summary>Builds the submit-ready envelope for <see cref="AdpOperationsClient.SubmitEventAsync"/>. Throws
    /// <see cref="ArgumentException"/> locally &#8212; before any HTTP call is attempted &#8212; when the subject
    /// worker's associate OID is missing, the allocation list is empty, or any allocation is missing its itemID.</summary>
    public static JsonElement Build(string associateOid, IReadOnlyList<OrganizationDepartment> departments)
    {
        if (string.IsNullOrWhiteSpace(associateOid)) throw new ArgumentException("The subject worker's associate OID is required.", nameof(associateOid));
        if (departments is null || departments.Count == 0) throw new ArgumentException("At least one organizational-unit allocation is required.", nameof(departments));

        var units = new object[departments.Count];
        for (var i = 0; i < departments.Count; i++)
        {
            var department = departments[i];
            if (string.IsNullOrWhiteSpace(department.ItemId)) throw new ArgumentException("Each allocation requires an itemID.", nameof(departments));
            var unit = new Dictionary<string, object?> { ["itemID"] = department.ItemId };
            if (!string.IsNullOrWhiteSpace(department.NameCode?.Code)) unit["nameCode"] = new Dictionary<string, object?> { ["codeValue"] = department.NameCode!.Code };
            units[i] = unit;
        }

        var envelope = new
        {
            events = new object[]
            {
                new
                {
                    data = new
                    {
                        eventContext = new { worker = new { associateOID = associateOid } },
                        transform = new { worker = new { workAssignment = new { assignedOrganizationalUnits = units } } }
                    }
                }
            }
        };
        return JsonSerializer.SerializeToElement(envelope);
    }
}
