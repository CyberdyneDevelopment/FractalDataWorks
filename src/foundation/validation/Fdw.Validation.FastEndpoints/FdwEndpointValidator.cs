using System;
using System.Linq;
using System.Linq.Expressions;
using FastEndpoints;
using FluentValidation;
using Fdw.Validation.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Validation.FastEndpoints;

/// <summary>
/// Base validator for FastEndpoints that provides common FDW validation rules.
/// Inherits from <see cref="Validator{TRequest}"/> for FastEndpoints auto-discovery
/// while providing the same convenience methods as <see cref="FdwValidator{T}"/>.
/// </summary>
/// <typeparam name="T">The request type being validated.</typeparam>
public abstract class FdwEndpointValidator<T> : Validator<T> where T : notnull
{
    /// <summary>
    /// Validates a name field: required, max length, starts with a letter, alphanumeric with hyphens/underscores.
    /// </summary>
    /// <param name="expression">The property expression.</param>
    /// <param name="maxLength">The maximum allowed length (default 200).</param>
    /// <returns>The rule builder for further chaining.</returns>
    protected IRuleBuilderOptions<T, string> ValidateName(
        Expression<Func<T, string>> expression, int maxLength = 200)
    {
        return RuleFor(expression)
            .NotEmpty()
            .MaximumLength(maxLength)
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Must start with a letter and contain only letters, numbers, underscores, or hyphens");
    }

    /// <summary>
    /// Validates an optional email address field.
    /// </summary>
    /// <param name="expression">The property expression.</param>
    /// <returns>The rule builder for further chaining.</returns>
    protected IRuleBuilderOptions<T, string?> ValidateEmail(
        Expression<Func<T, string?>> expression)
    {
        return RuleFor(expression)
            .EmailAddress()
            .When(x => expression.Compile()(x) is not null, ApplyConditionTo.CurrentValidator)
            .MaximumLength(320);
    }

    /// <summary>
    /// Validates a GUID identifier: must not be <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="expression">The property expression.</param>
    /// <returns>The rule builder for further chaining.</returns>
    protected IRuleBuilderOptions<T, Guid> ValidateId(
        Expression<Func<T, Guid>> expression)
    {
        return RuleFor(expression)
            .NotEqual(Guid.Empty)
            .WithMessage("A valid ID is required");
    }

    /// <summary>
    /// Validates a password field: required, minimum length.
    /// </summary>
    /// <param name="expression">The property expression.</param>
    /// <param name="minLength">The minimum allowed length (default 8).</param>
    /// <returns>The rule builder for further chaining.</returns>
    protected IRuleBuilderOptions<T, string> ValidatePassword(
        Expression<Func<T, string>> expression, int minLength = 8)
    {
        return RuleFor(expression)
            .NotEmpty()
            .MinimumLength(minLength);
    }

    /// <summary>
    /// Takes the rules of a configuration from the validator that configuration class declared (FDW-3884), so a rule about it has one
    /// source: the request answers 400 for exactly what the save check would refuse.
    /// </summary>
    /// <typeparam name="TConfiguration">The configuration class the request describes.</typeparam>
    /// <param name="describe">Builds the configuration the request describes, from the request's own members.</param>
    /// <remarks>
    /// The validator is found the way the save check finds it (<see cref="ConfigurationValidatorResolver.Find"/>), so a class that declares
    /// none is a failure here too, never a pass. Only a failure on a member the request carries is reported: a request that has no Name is
    /// not refused for a Name it cannot state. Each failure is reported under the member's own name.
    /// </remarks>
    protected void RulesOf<TConfiguration>(Func<T, TConfiguration> describe)
        where TConfiguration : notnull
    {
        RuleFor(request => request).Custom((request, context) =>
        {
            var found = ConfigurationValidatorResolver.Find(typeof(TConfiguration), NullLogger.Instance);
            if (found.IsFailure)
            {
                context.AddFailure(typeof(TConfiguration).Name, found.CurrentMessage ?? string.Empty);
                return;
            }

            if (found.Value!.Validator is not { } validator)
                return;

            var validated = validator.Validate(new FluentValidation.ValidationContext<object>(describe(request)));
            foreach (var failure in validated.Errors.Where(failure => CarriedByRequest(failure.PropertyName)))
                context.AddFailure(failure.PropertyName, failure.ErrorMessage);
        });
    }

    private static bool CarriedByRequest(string propertyName) =>
        typeof(T).GetProperty(propertyName.Split('.', '[')[0]) is not null;
}
