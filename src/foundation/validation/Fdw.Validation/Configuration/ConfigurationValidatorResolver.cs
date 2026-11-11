using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Validation.Configuration;

/// <summary>
/// Finds the option of <see cref="ConfigurationValidators"/> that speaks for a configuration class (FDW-3541).
/// </summary>
/// <remarks>
/// The class itself is looked up first, then its base classes nearest first, then the interfaces it implements. The first stage that
/// finds an option decides; at the interface stage exactly one option may match. Finding none, or several at one stage, is a refusal: the
/// write is never passed on a guess.
/// </remarks>
public static class ConfigurationValidatorResolver
{
    // Why ByName and not a lookup of its own: the generator emits a [TypeLookup] method only from the options its own compilation sees, and these options
    // are declared in other assemblies; ByName is the one lookup it always emits. An option is named for its configuration class's short name, so the
    // class it speaks for is compared exactly: another class with the same short name does not match.
    private static IConfigurationValidator? Speaking(Type type)
    {
        var option = ConfigurationValidators.ByName(type.Name);
        return !ReferenceEquals(option, ConfigurationValidators.NotFound) && option.ConfigurationType == type ? option : null;
    }

    /// <summary>Finds the option that speaks for a configuration class.</summary>
    /// <param name="configurationType">The class being written.</param>
    /// <param name="logger">The logger a refusal is logged to.</param>
    /// <returns>The option, or a coded failure when none or several speak for the class.</returns>
    public static IGenericResult<IConfigurationValidator> Find(Type configurationType, ILogger logger)
    {
        for (var type = configurationType; type is not null; type = type.BaseType)
        {
            var option = Speaking(type);
            if (option is not null)
                return GenericResult<IConfigurationValidator>.Success(option);
        }

        var matches = configurationType.GetInterfaces()
            .Select(Speaking)
            .OfType<IConfigurationValidator>()
            .ToList();

        return matches.Count switch
        {
            1 => GenericResult<IConfigurationValidator>.Success(matches[0]),
            0 => GenericResult<IConfigurationValidator>.Failure(
                ConfigurationValidatorLog.NoValidatorDeclared(logger, configurationType.Name)),
            _ => GenericResult<IConfigurationValidator>.Failure(
                ConfigurationValidatorLog.AmbiguousValidatorDeclared(
                    logger, configurationType.Name, string.Join(", ", matches.Select(option => option.Name).OrderBy(name => name, StringComparer.Ordinal)))),
        };
    }
}
