namespace Koras.Connect.Abstractions;

/// <summary>A single field-level mapping failure. Never carries raw source values by default.</summary>
public sealed record FieldError(string Field, string Reason);

/// <summary>Distinguishes a mapped value from an explicit omission or field-level errors; never silently drops information.</summary>
public sealed class MappingResult<TTarget>
{
    public TTarget? Value { get; }
    public string? OmittedReason { get; }
    public IReadOnlyList<FieldError> Errors { get; }

    public bool IsSuccess => Errors.Count == 0 && OmittedReason is null;
    public bool IsOmitted => OmittedReason is not null;

    private MappingResult(TTarget? value, string? omittedReason, IReadOnlyList<FieldError> errors)
    { Value = value; OmittedReason = omittedReason; Errors = errors; }

    public static MappingResult<TTarget> Success(TTarget value) => new(value, null, []);
    public static MappingResult<TTarget> Omitted(string reason) => new(default, reason, []);
    public static MappingResult<TTarget> Failure(IReadOnlyList<FieldError> errors) => new(default, null, errors);
}

/// <summary>Maps a provider-specific source record to a neutral target type.</summary>
public interface IRecordMapper<TSource, TTarget>
{
    MappingResult<TTarget> Map(TSource source);
}
