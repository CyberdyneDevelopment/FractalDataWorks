using System;
using System.Linq;
using FluentValidation;
using Fdw.Services.Authorization.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Authorization.Validation;

/// <summary>
/// Validator for <see cref="PermissionConfiguration"/>.
/// </summary>
public sealed class PermissionConfigurationValidator : FdwConfigurationValidator<PermissionConfiguration>
{
    private static readonly string[] ValidActions = ["read", "write", "execute", "delete", "admin"];

    // Why: Scope replaces the old RequiresTenant boolean. Three values capture the full
    // permission visibility model: tenant-scoped, system-wide, or global (both).
    private static readonly string[] ValidScopes = ["tenant", "system", "global"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionConfigurationValidator"/> class.
    /// </summary>
    public PermissionConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        RuleFor(x => x.Domain)
            .NotEmpty()
            .WithMessage("Domain is required")
            .MaximumLength(100)
            .WithMessage("Domain must not exceed 100 characters")
            .Matches(@"^[a-z][a-z0-9_-]*$")
            .WithMessage("Domain must start with a lowercase letter and contain only lowercase letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Resource)
            .NotEmpty()
            .WithMessage("Resource is required")
            .MaximumLength(100)
            .WithMessage("Resource must not exceed 100 characters")
            .Matches(@"^(\*|[a-z][a-z0-9_-]*)$")
            .WithMessage("Resource must be '*' or start with a lowercase letter and contain only lowercase letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Action is required")
            .MaximumLength(50)
            .WithMessage("Action must not exceed 50 characters")
            .Must(action => ValidActions.Contains(action, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Action must be one of: {string.Join(", ", ValidActions)}");

        RuleFor(x => x.Scope)
            .NotEmpty()
            .WithMessage("Scope is required")
            .Must(scope => ValidScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Scope must be one of: {string.Join(", ", ValidScopes)}");

        When(x => x.Category is not null, () =>
        {
            RuleFor(x => x.Category!)
                .MaximumLength(100)
                .WithMessage("Category must not exceed 100 characters");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SortOrder must be greater than or equal to 0");
    }
}
