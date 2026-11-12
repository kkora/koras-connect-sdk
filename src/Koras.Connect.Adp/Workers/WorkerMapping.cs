using Koras.Connect.Abstractions;
namespace Koras.Connect.Adp.Workers;

/// <summary>Neutral worker identity projection. Fields not selected here remain on the source <see cref="Worker"/>
/// unchanged; this mapper never mutates or discards the source model, it only reports what it could project.</summary>
public sealed record WorkerIdentity(string AssociateOid, string GivenName, string FamilyName, string? ExternalId);

/// <summary>Maps a raw <see cref="Worker"/> to a neutral <see cref="WorkerIdentity"/>. A worker with no legal name on
/// record is <see cref="MappingResult{TTarget}.Omitted"/> rather than guessed; a worker with a legal name but missing
/// required components reports <see cref="FieldError"/>s instead of silently defaulting. A masked or absent
/// <see cref="Identifier"/> is preserved as <c>null</c>, not fabricated.</summary>
public sealed class WorkerIdentityMapper : IRecordMapper<Worker, WorkerIdentity>
{
    public MappingResult<WorkerIdentity> Map(Worker source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var legalName = source.Person?.LegalName;
        if (legalName is null) return MappingResult<WorkerIdentity>.Omitted("Worker has no legal name on record.");

        var errors = new List<FieldError>();
        if (string.IsNullOrWhiteSpace(source.AssociateOid)) errors.Add(new("associateOID", "Missing associate OID."));
        if (string.IsNullOrWhiteSpace(legalName.GivenName)) errors.Add(new("person.legalName.givenName", "Missing given name."));
        if (string.IsNullOrWhiteSpace(legalName.FamilyName)) errors.Add(new("person.legalName.familyName1", "Missing family name."));
        if (errors.Count > 0) return MappingResult<WorkerIdentity>.Failure(errors);

        // A masked or unassigned workerID is valid tenant behavior, not a mapping error; it is simply absent downstream.
        var externalId = source.WorkerId?.Value;
        return MappingResult<WorkerIdentity>.Success(new WorkerIdentity(source.AssociateOid!, legalName.GivenName!, legalName.FamilyName!, externalId));
    }
}

/// <summary>Selects the work assignment that is in effect on a given date from the documented
/// hireDate/terminationDate fields. Never infers effectiveness from array position; a worker with multiple
/// overlapping assignments resolves to whichever has the latest hireDate that is still on or before the target
/// date, and whose terminationDate (if any) is on or after the target date.</summary>
public static class EffectiveAssignmentSelector
{
    /// <summary>Returns the assignment in effect on <paramref name="asOf"/>, or <c>null</c> if none qualifies.</summary>
    public static WorkAssignment? SelectEffective(IEnumerable<WorkAssignment> assignments, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        WorkAssignment? best = null;
        DateOnly bestHire = default;
        foreach (var assignment in assignments)
        {
            if (!TryParse(assignment.HireDate, out var hire) || hire > asOf) continue;
            if (TryParse(assignment.TerminationDate, out var termination) && termination < asOf) continue;
            if (best is null || hire >= bestHire) { best = assignment; bestHire = hire; }
        }
        return best;
    }

    private static bool TryParse(string? value, out DateOnly date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(value)
            && DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
    }
}
