using System;
using FluentValidation;
using Fdw.Services.Authorization.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Authorization.Validation;

/// <summary>
/// Validator for <see cref="RoleConfiguration"/>.
/// </summary>
public sealed class RoleConfigurationValidator : FdwConfigurationValidator<RoleConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleConfigurationValidator"/> class.
    /// </summary>
    public RoleConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(100);

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .MaximumLength(200)
                .WithMessage("DisplayName must not exceed 200 characters");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SortOrder must be greater than or equal to 0");

        When(x => x.IsTenantScoped && x.TenantId.HasValue, () =>
        {
            RuleFor(x => x.TenantId!.Value)
                .NotEqual(Guid.Empty)
                .WithMessage("TenantId must be a valid GUID when IsTenantScoped is true");
        });
    }
}
