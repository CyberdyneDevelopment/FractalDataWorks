using System;
using FluentValidation;
using Fdw.Services.Settings.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Settings.Validation;

/// <summary>
/// Validator for <see cref="TenantSettingConfiguration"/>.
/// </summary>
public sealed class TenantSettingConfigurationValidator : FdwConfigurationValidator<TenantSettingConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSettingConfigurationValidator"/> class.
    /// </summary>
    public TenantSettingConfigurationValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty)
            .WithMessage("TenantId is required");

        RuleFor(x => x.SettingName)
            .IsValidName(256);

        RuleFor(x => x.SettingValue)
            .NotEmpty()
            .WithMessage("SettingValue is required");
    }
}
