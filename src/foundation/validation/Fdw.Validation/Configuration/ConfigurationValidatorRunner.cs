using System;
using FluentValidation;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Validation.Configuration;

/// <summary>
/// Checks a configuration with the validator its class declares, before the configuration is written (FDW-3541).
/// </summary>
/// <remarks>
/// A class that states <see cref="IConfigurationValidator.NoValidationReason"/> passes. A class with no declaration is refused by
/// <see cref="ConfigurationValidatorResolver"/>, and a configuration its validator refuses is one failure with a message for each rule that has a
/// coded message of its own (it keeps its code and client text) and one message naming the other rules, so a host answers it with 400 (FDW-4738).
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
            : GenericResult.Failure(ValidationFailureMessages.From(
                validated,
                details => ConfigurationValidatorLog.ConfigurationRejected(logger, configuration.GetType().Name, details)));
    }
}
