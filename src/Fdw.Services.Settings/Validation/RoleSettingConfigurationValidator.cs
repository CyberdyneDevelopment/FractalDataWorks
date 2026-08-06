using System;
using FluentValidation;
using Fdw.Services.Settings.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Settings.Validation;

/// <summary>
/// Validator for <see cref="RoleSettingConfiguration"/>.
/// </summary>
public sealed class RoleSettingConfigurationValidator : FdwConfigurationValidator<RoleSettingConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleSettingConfigurationValidator"/> class.
    /// </summary>
    public RoleSettingConfigurationValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty)
            .WithMessage("TenantId is required");

        RuleFor(x => x.RoleName)
            .IsValidName(256);

        RuleFor(x => x.SettingName)
            .IsValidName(256);

        RuleFor(x => x.SettingValue)
            .NotEmpty()
            .WithMessage("SettingValue is required");
    }
}
