using System;
using System.Linq;
using FluentValidation;
using Fdw.Services.Authorization.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Authorization.Validation;

/// <summary>
/// Validator for <see cref="PermissionImplementationConfiguration"/>. The grammar is the one the platform's permissions are stored in (FDW-4241): a name
/// is <c>resource:action</c>; a resource is <c>*</c> or slash-separated segments (<c>settings/role</c>, <c>workspaces/{guid}</c>); an action is a lowercase
/// token that the domain that owns the permission declares (<c>read</c>, <c>delegate</c>, <c>members</c>, <c>view</c>, <c>query</c> ...), so no closed list
/// of actions lives here; the permission's own domain is <c>PermissionDomain</c>, and <c>Domain</c> is the record kind (<c>Permission</c>).
/// Checked against every current permission of TechComConfigurationDb (157 rows) when it was written.
/// </summary>
public sealed class PermissionConfigurationValidator : FdwConfigurationValidator<PermissionImplementationConfiguration>
{
    private static readonly string[] ValidScopes = ["tenant", "system", "global"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionConfigurationValidator"/> class.
    /// </summary>
    public PermissionConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters")
            .Matches(@"^[a-zA-Z*][a-zA-Z0-9_/:*-]*$")
            .WithMessage("Name must start with a letter or '*' and contain only letters, numbers, underscores, hyphens, '/', ':' or '*'");

        RuleFor(x => x.PermissionDomain)
            .NotEmpty()
            .WithMessage("PermissionDomain is required")
            .MaximumLength(100)
            .WithMessage("PermissionDomain must not exceed 100 characters")
            .Matches(@"^[a-z][a-z0-9_-]*$")
            .WithMessage("PermissionDomain must start with a lowercase letter and contain only lowercase letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Resource)
            .NotEmpty()
            .WithMessage("Resource is required")
            .MaximumLength(100)
            .WithMessage("Resource must not exceed 100 characters")
            .Matches(@"^(\*|[a-z][a-z0-9_-]*(/[a-z0-9][a-z0-9_-]*)*)$")
            .WithMessage("Resource must be '*' or slash-separated segments: the first starts with a lowercase letter, each other starts with a lowercase letter or a number, and they contain only lowercase letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Action is required")
            .MaximumLength(50)
            .WithMessage("Action must not exceed 50 characters")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Action must start with a letter and contain only letters, numbers, underscores, or hyphens");

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
    }
}
