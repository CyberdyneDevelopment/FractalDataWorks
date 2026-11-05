using System;
using System.Linq;
using FluentValidation;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Validation.Configuration;

/// <summary>
/// Checks a configuration with the validator its class declares, before the configuration is written (FDW-3541).
/// </summary>
/// <remarks>
/// A class that states <see cref="IConfigurationValidator.NoValidationReason"/> passes. A class with no declaration is refused by
/// <see cref="ConfigurationValidatorResolver"/>, and a configuration its validator refuses is one failure naming every failed rule, with a client
/// message, so a host answers it with 400.
/// </remarks>
public static class ConfigurationValidatorRunner
{
    /// <summary>Checks a configuration.</summary>
    /// <param name="configuration">The configuration about to be written.</param>
    /// <param name="logger">The logger a refusal is logged to.</param>
    /// <returns>Success, or a coded failure when no option speaks for the class or its validator refuses the configuration.</returns>
    public static IGenericResult Validate(object configuration, ILogger logger)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var found = ConfigurationValidatorResolver.Find(configuration.GetType(), logger);
        if (found.IsFailure)
            return found;

        if (found.Value!.Validator is not { } validator)
            return GenericResult.Success();

        var validated = validator.Validate(new ValidationContext<object>(configuration));
        return validated.IsValid
            ? GenericResult.Success()
            : GenericResult.Failure(ConfigurationValidatorLog.ConfigurationRejected(
                logger,
                configuration.GetType().Name,
                string.Join("; ", validated.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"))));
    }
}
