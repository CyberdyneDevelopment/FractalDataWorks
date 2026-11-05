using System;
using FluentValidation;
using Fdw.Collections;

namespace Fdw.Validation.Configuration;

/// <summary>
/// What a configuration class states about its own validation: the validator that checks it, or the reason it needs none (FDW-3541).
/// </summary>
public interface IConfigurationValidator : ITypeOption<int, ConfigurationValidatorBase>
{
    /// <summary>Gets the configuration class this option speaks for; a base class or an interface covers what derives from or implements it.</summary>
    Type ConfigurationType { get; }

    /// <summary>Gets the configuration class's full name, which the options are looked up by.</summary>
    string ConfigurationName { get; }

    /// <summary>Gets the validator that checks the configuration, or null when the option states <see cref="NoValidationReason"/> instead.</summary>
    IValidator? Validator { get; }

    /// <summary>Gets why the configuration needs no validator, or null when the option holds a <see cref="Validator"/>.</summary>
    string? NoValidationReason { get; }
}
