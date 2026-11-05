using System;
using FluentValidation;
using Fdw.Collections;

namespace Fdw.Validation.Configuration;

/// <summary>
/// Base class for the options of <see cref="ConfigurationValidators"/>. Each option speaks for one configuration class and either holds the
/// validator that checks it or states why it needs none, so a configuration class that does neither is a defect the write refuses (FDW-3541).
/// </summary>
/// <remarks>
/// The validator is an abstract member, not a constructor parameter: the generated NotFound sentinel is built from defaultable parameters
/// only, and a null passed to a guard here would stop every host at start (FDW-2931).
/// </remarks>
public abstract class ConfigurationValidatorBase : TypeOptionBase<ConfigurationValidatorBase>, IConfigurationValidator
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationValidatorBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="configurationType">The configuration class this option speaks for.</param>
    /// <param name="configurationName">The configuration class's name.</param>
    protected ConfigurationValidatorBase(string name, Type configurationType, string configurationName)
        : base(name)
    {
        ConfigurationType = configurationType;
        ConfigurationName = configurationName;
    }

    /// <inheritdoc />
    public Type ConfigurationType { get; }

    /// <inheritdoc />
    public string ConfigurationName { get; }

    /// <summary>Gets the name an option is looked up by for a configuration class.</summary>
    /// <param name="configurationType">The configuration class, base class or interface.</param>
    /// <returns>The type's full name.</returns>
    /// <exception cref="InvalidOperationException">The type has no full name (an open generic parameter), so no option can speak for it.</exception>
    public static string NameOf(Type configurationType)
        => configurationType.FullName ?? throw new InvalidOperationException(
            $"'{configurationType}' has no full name, so no configuration validator option can be declared or looked up for it.");

    /// <inheritdoc />
    public abstract IValidator? Validator { get; }

    /// <inheritdoc />
    public abstract string? NoValidationReason { get; }
}
