using System;

namespace Fdw.Validation.Configuration;

/// <summary>
/// An option that says a configuration class needs no validator, and why. This is a recorded decision: a configuration class with neither a
/// validator nor this is refused when it is written, never passed.
/// </summary>
/// <typeparam name="TConfiguration">The configuration class (or its base class or interface) that needs no validator.</typeparam>
public abstract class UnvalidatedConfiguration<TConfiguration> : ConfigurationValidatorBase
    where TConfiguration : class
{
    private readonly string _reason;

    /// <summary>Initializes a new instance of the <see cref="UnvalidatedConfiguration{TConfiguration}"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="reason">Why the configuration class needs no validator.</param>
    protected UnvalidatedConfiguration(string name, string reason)
        : base(name, typeof(TConfiguration), NameOf(typeof(TConfiguration)))
    {
        _reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("A reason is required.", nameof(reason)) : reason;
    }

    /// <inheritdoc />
    public sealed override FluentValidation.IValidator? Validator => null;

    /// <inheritdoc />
    public sealed override string? NoValidationReason => _reason;
}
