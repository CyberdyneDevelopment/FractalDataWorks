using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Fdw.Validation;

/// <summary>
/// Base validator for configuration classes that integrates FluentValidation with
/// <see cref="IValidateOptions{TOptions}"/> for startup validation.
/// </summary>
/// <typeparam name="T">The configuration type to validate.</typeparam>
public abstract class FdwConfigurationValidator<T> : AbstractValidator<T>, IValidateOptions<T>
    where T : class
{
    /// <summary>
    /// Validates the configuration options using FluentValidation rules.
    /// Called by the options framework when <c>ValidateOnStart</c> is enabled.
    /// </summary>
    /// <param name="name">The options name being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = Validate(options);
        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = result.Errors.Select(e => e.ErrorMessage).ToArray();
        return ValidateOptionsResult.Fail(errors);
    }
}
