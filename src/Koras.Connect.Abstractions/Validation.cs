namespace Koras.Connect.Abstractions;

/// <summary>Options that validate the same way whether constructed directly or through dependency injection.</summary>
public interface IValidatableOptions
{
    /// <summary>Throws <see cref="OptionsValidationException"/> describing every violated field.</summary>
    void Validate();
}

/// <summary>Field-level configuration validation failure. Never includes secret values.</summary>
public sealed class OptionsValidationException : Exception
{
    public IReadOnlyList<string> Failures { get; }

    public OptionsValidationException(string optionsTypeName, IReadOnlyList<string> failures)
        : base($"{optionsTypeName} failed validation: {string.Join("; ", failures)}")
        => Failures = failures;

    public OptionsValidationException(string optionsTypeName, string failure)
        : this(optionsTypeName, [failure]) { }
}
