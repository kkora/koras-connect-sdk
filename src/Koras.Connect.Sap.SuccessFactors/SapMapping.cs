using System.Text.Json;
using Koras.Connect.Abstractions;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>Neutral, provider-independent projection of a <see cref="PerPerson"/> record.</summary>
public sealed record PersonProfile(string PersonId, DateTimeOffset? DateOfBirth);

/// <summary>Maps <see cref="PerPerson"/> to <see cref="PersonProfile"/> via <see cref="Koras.Connect.Abstractions.IRecordMapper{TSource,TTarget}"/>.
/// Prefers the host-assigned external identifier (<see cref="PerPerson.PersonIdExternal"/>) over the internal
/// <see cref="PerPerson.PersonId"/>; a record with neither is explicitly omitted rather than mapped with a null key.
/// Date parsing follows the <see cref="SapDates"/> policy: a present-but-unrecognized date value is a field-level
/// <see cref="FieldError"/>, never a silently-dropped null.</summary>
public sealed class PerPersonMapper : IRecordMapper<PerPerson, PersonProfile>
{
    public MappingResult<PersonProfile> Map(PerPerson source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var id = !string.IsNullOrWhiteSpace(source.PersonIdExternal) ? source.PersonIdExternal : source.PersonId;
        if (string.IsNullOrWhiteSpace(id)) return MappingResult<PersonProfile>.Omitted("Missing both personIdExternal and personId.");
        try
        {
            var dateOfBirth = SapDates.ParseUtcOrNull(source.DateOfBirth);
            return MappingResult<PersonProfile>.Success(new PersonProfile(id, dateOfBirth));
        }
        catch (FormatException ex)
        {
            return MappingResult<PersonProfile>.Failure([new FieldError("dateOfBirth", ex.Message)]);
        }
    }
}

/// <summary>Neutral, provider-independent projection of an <see cref="EmpEmployment"/> record. <see cref="Key"/>
/// is the documented composite key (userId + startDate) joined with a pipe, matching the identifier the SAP
/// OData entity itself uses - a record cannot be uniquely addressed by <see cref="UserId"/> alone.</summary>
public sealed record EmploymentRecord(string Key, string UserId, string? PersonIdExternal, DateTimeOffset StartDate,
    DateTimeOffset? EndDate, IReadOnlyDictionary<string, JsonElement> Extensions);

/// <summary>Maps <see cref="EmpEmployment"/> to <see cref="EmploymentRecord"/>. A record missing <c>userId</c>
/// carries no identifier at all and is omitted; a record with a <c>userId</c> but a missing or unparseable
/// <c>startDate</c> cannot form its documented composite key and fails with a field-level <see cref="FieldError"/>
/// rather than inventing a partial key.</summary>
public sealed class EmpEmploymentMapper : IRecordMapper<EmpEmployment, EmploymentRecord>
{
    public MappingResult<EmploymentRecord> Map(EmpEmployment source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.UserId)) return MappingResult<EmploymentRecord>.Omitted("Missing userId.");
        DateTimeOffset startDate;
        try
        {
            var parsed = SapDates.ParseUtcOrNull(source.StartDate);
            if (parsed is null) return MappingResult<EmploymentRecord>.Failure([new FieldError("startDate", "startDate is required to form the composite key (userId + startDate).")]);
            startDate = parsed.Value;
        }
        catch (FormatException ex) { return MappingResult<EmploymentRecord>.Failure([new FieldError("startDate", ex.Message)]); }
        DateTimeOffset? endDate;
        try { endDate = SapDates.ParseUtcOrNull(source.EndDate); }
        catch (FormatException ex) { return MappingResult<EmploymentRecord>.Failure([new FieldError("endDate", ex.Message)]); }
        var extensions = source.AdditionalData is null ? new Dictionary<string, JsonElement>() : new Dictionary<string, JsonElement>(source.AdditionalData);
        var key = $"{source.UserId}|{startDate:O}";
        return MappingResult<EmploymentRecord>.Success(new EmploymentRecord(key, source.UserId, source.PersonIdExternal, startDate, endDate, extensions));
    }
}

/// <summary>Neutral, provider-independent projection of an <see cref="EmpJob"/> record. <see cref="Key"/> is the
/// documented composite key (userId + seqNumber) joined with a pipe - <see cref="EmpJob.SequenceNumber"/>, not
/// <see cref="EmpJob.StartDate"/>, is the field that disambiguates multiple job records sharing the same effective
/// date (see SAP-009's same-day-sequence handling).</summary>
public sealed record JobRecord(string Key, string UserId, long SequenceNumber, DateTimeOffset? StartDate,
    string? ManagerId, string? Department, string? Location, string? JobTitle, IReadOnlyDictionary<string, JsonElement> Extensions);

/// <summary>Maps <see cref="EmpJob"/> to <see cref="JobRecord"/>. A record missing <c>userId</c> carries no
/// identifier at all and is omitted; a record with a <c>userId</c> but no <c>seqNumber</c> cannot form its
/// documented composite key and fails with a field-level <see cref="FieldError"/>. Unlike <see cref="EmpEmployment"/>'s
/// <c>startDate</c>, <see cref="EmpJob.StartDate"/> is not part of the key and may be legitimately absent/unparseable
/// without failing the whole mapping - it is reported as <c>null</c>, matching <see cref="SuccessFactorsClient.ToManagerAssignment"/>'s
/// existing "unknown effective date is not an error" policy for this same field.</summary>
public sealed class EmpJobMapper : IRecordMapper<EmpJob, JobRecord>
{
    public MappingResult<JobRecord> Map(EmpJob source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.UserId)) return MappingResult<JobRecord>.Omitted("Missing userId.");
        if (source.SequenceNumber is null) return MappingResult<JobRecord>.Failure([new FieldError("seqNumber", "seqNumber is required to form the composite key (userId + seqNumber).")]);
        DateTimeOffset? startDate;
        try { startDate = SapDates.ParseUtcOrNull(source.StartDate); }
        catch (FormatException) { startDate = null; }
        var extensions = source.AdditionalData is null ? new Dictionary<string, JsonElement>() : new Dictionary<string, JsonElement>(source.AdditionalData);
        var key = $"{source.UserId}|{source.SequenceNumber}";
        return MappingResult<JobRecord>.Success(new JobRecord(key, source.UserId, source.SequenceNumber.Value, startDate,
            source.ManagerId, source.Department, source.Location, source.JobTitle, extensions));
    }
}
