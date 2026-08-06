using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Fdw.Collections;
using Fdw.Conventions;

namespace Fdw.Validation;

/// <summary>
/// Reusable validation rules for the Fdw framework.
/// </summary>
public static class FdwValidationRules
{
    /// <summary>
    /// Validates a name: starts with a letter, alphanumeric with hyphens/underscores, max length.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="maxLength">The maximum allowed length (default 200).</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, string> IsValidName<T>(
        this IRuleBuilder<T, string> rule, int maxLength = 200)
    {
        return rule
            .NotEmpty()
            .MaximumLength(maxLength)
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Must start with a letter and contain only letters, numbers, underscores, or hyphens");
    }

    /// <summary>
    /// Validates a connection string: not empty, no obvious injection patterns.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, string> IsValidConnectionString<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty()
            .WithMessage("Connection string is required")
            .Must(value => !ContainsSqlInjectionPatterns(value))
            .WithMessage("Connection string contains suspicious patterns");
    }

    /// <summary>
    /// Validates a cron expression: 5 or 6 space-separated parts with valid cron characters.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, string> IsValidCronExpression<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty()
            .Must(BeValidCronExpression)
            .WithMessage("Invalid cron expression format. Expected: minute hour dayOfMonth month dayOfWeek");
    }

    /// <summary>
    /// Validates that a GUID is not <see cref="Guid.Empty"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, Guid> IsNotEmpty<T>(
        this IRuleBuilder<T, Guid> rule)
    {
        return rule
            .NotEqual(Guid.Empty)
            .WithMessage("A valid ID is required");
    }

    /// <summary>
    /// Validates a string is safe: no control characters, reasonable length.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="maxLength">The maximum allowed length (default 4000).</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, string> IsSafeString<T>(
        this IRuleBuilder<T, string> rule, int maxLength = 4000)
    {
        return rule
            .MaximumLength(maxLength)
            .Must(value => string.IsNullOrEmpty(value) || !ContainsControlCharacters(value))
            .WithMessage("Value contains invalid control characters");
    }

    /// <summary>
    /// Validates that a string value matches the Name of an option in the given TypeCollection.
    /// Use this for [ValuesFrom] properties that reference a TypeCollection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="typeCollectionAccessor">A function returning all type options (e.g., <c>() => MyTypes.All()</c>).</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, string> MustExistIn<T>(
        this IRuleBuilder<T, string> rule,
        Func<IReadOnlyCollection<ITypeOption>> typeCollectionAccessor)
    {
        return rule
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true; // Why: Empty values are handled by NotEmpty — this rule only checks membership.

                var options = typeCollectionAccessor();
                return options.Any(o => string.Equals(o.Name, value, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage("'{PropertyName}' must be a valid type option. '{PropertyValue}' was not found.");
    }

    /// <summary>
    /// Validates that a parent configuration exists by checking a provider lookup.
    /// Use this for FK properties that reference another configuration's Name.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TParent">The parent configuration type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="parentLookup">A function that looks up the parent by name, returning null if not found.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, string> ParentMustExist<T, TParent>(
        this IRuleBuilder<T, string> rule,
        Func<string, TParent?> parentLookup)
        where TParent : class
    {
        return rule
            .Must(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true; // Why: Empty values are handled by NotEmpty — this rule only checks existence.

                return parentLookup(value) is not null;
            })
            .WithMessage("'{PropertyName}' references '{PropertyValue}' which does not exist.");
    }

    /// <summary>
    /// Validates that a parent configuration exists by checking a provider lookup using a GUID ID.
    /// Use this for FK properties that reference another configuration's ID.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TParent">The parent configuration type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="parentLookup">A function that looks up the parent by ID, returning null if not found.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilderOptions<T, Guid> ParentMustExist<T, TParent>(
        this IRuleBuilder<T, Guid> rule,
        Func<Guid, TParent?> parentLookup)
        where TParent : class
    {
        return rule
            .Must(value =>
            {
                if (value == Guid.Empty)
                    return true; // Why: Empty GUIDs are handled by IsNotEmpty — this rule only checks existence.

                return parentLookup(value) is not null;
            })
            .WithMessage("'{PropertyName}' references an ID that does not exist.");
    }

    private static bool ContainsSqlInjectionPatterns(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Check for common SQL injection patterns in connection strings
        var suspicious = new[]
        {
            ";--", "'; ", "1=1", "' OR ", "' AND ",
            "xp_", "sp_", "EXEC ", "EXECUTE ", "DROP ", "DELETE ", "INSERT ", "UPDATE "
        };

        foreach (var pattern in suspicious)
        {
            if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsControlCharacters(string value)
    {
        foreach (var c in value)
        {
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
            {
                return true;
            }
        }

        return false;
    }

    private static bool BeValidCronExpression(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Standard cron has 5 parts, Quartz-style has 6 (with seconds)
        if (parts.Length is < 5 or > 6)
        {
            return false;
        }

        foreach (var part in parts)
        {
            if (!IsValidCronPart(part))
            {
                return false;
            }
        }

        return true;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Cron validation — independent character checks
    private static bool IsValidCronPart(string part)
    {
        foreach (var c in part)
        {
            if (!char.IsDigit(c) &&
                c != '*' && c != '-' && c != '/' && c != ',' &&
                c != 'L' && c != 'W' && c != '#' && c != '?')
            {
                return false;
            }
        }

        return true;
    }
}
