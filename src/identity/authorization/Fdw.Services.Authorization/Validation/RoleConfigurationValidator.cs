using System;
using FluentValidation;
using Fdw.Services.Authorization.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Authorization.Validation;

/// <summary>
/// Validator for <see cref="RoleImplementationConfiguration"/>.
/// </summary>
public sealed class RoleConfigurationValidator : FdwConfigurationValidator<RoleImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleConfigurationValidator"/> class.
    /// </summary>
    public RoleConfigurationValidator()
    {
        // Why a minimum of three here and not only on the create request: a role named "ab" is the same mistake however it is saved, so the
        // rule has one source (FDW-3884).
        RuleFor(x => x.Name)
            .MinimumLength(3)
            .WithMessage("Role name must be at least 3 characters")
            .IsValidName(100);

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .MaximumLength(200)
                .WithMessage("DisplayName must not exceed 200 characters");
        });

        When(x => x.Description is not null, () =>
        {
            // Why 500: authz.Role.Description is VARCHAR(500), so a longer description is refused here and not by a failed insert (FDW-3932).
            RuleFor(x => x.Description!)
                .IsSafeString(500);
        });

        When(x => x.IsTenantScoped && x.TenantId.HasValue, () =>
        {
            RuleFor(x => x.TenantId!.Value)
                .NotEqual(Guid.Empty)
                .WithMessage("TenantId must be a valid GUID when IsTenantScoped is true");
        });
    }
}
