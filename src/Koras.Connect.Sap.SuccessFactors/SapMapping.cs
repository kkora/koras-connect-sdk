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
