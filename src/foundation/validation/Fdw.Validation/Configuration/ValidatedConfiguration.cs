using System;
using FluentValidation;

namespace Fdw.Validation.Configuration;

/// <summary>
/// An option that says a configuration class is checked by a validator.
/// </summary>
/// <typeparam name="TConfiguration">The configuration class (or its base class or interface) the validator checks.</typeparam>
public abstract class ValidatedConfiguration<TConfiguration> : ConfigurationValidatorBase
    where TConfiguration : class
{
    private readonly IValidator<TConfiguration> _validator;

    /// <summary>Initializes a new instance of the <see cref="ValidatedConfiguration{TConfiguration}"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="validator">The validator that checks the configuration.</param>
    protected ValidatedConfiguration(string name, IValidator<TConfiguration> validator)
        : base(name, typeof(TConfiguration), NameOf(typeof(TConfiguration)))
    {
        _validator = validator;
    }

    /// <inheritdoc />
    public sealed override IValidator? Validator => _validator;

    /// <inheritdoc />
    public sealed override string? NoValidationReason => null;
}
