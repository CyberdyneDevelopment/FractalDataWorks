using FluentValidation;
using Fdw.Services.Settings.Configuration;
using Fdw.Validation;

namespace Fdw.Services.Settings.Validation;

/// <summary>
/// Validator for <see cref="ServerSettingConfiguration"/>.
/// </summary>
public sealed class ServerSettingConfigurationValidator : FdwConfigurationValidator<ServerSettingConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerSettingConfigurationValidator"/> class.
    /// </summary>
    public ServerSettingConfigurationValidator()
    {
        RuleFor(x => x.SettingName)
            .IsValidName(256);

        RuleFor(x => x.SettingValue)
            .NotEmpty()
            .WithMessage("SettingValue is required");

        RuleFor(x => x.DataType)
            .NotEmpty()
            .WithMessage("DataType is required")
            .MaximumLength(64)
            .WithMessage("DataType must not exceed 64 characters");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1024);
        });

        When(x => x.MinValue is not null, () =>
        {
            RuleFor(x => x.MinValue!)
                .MaximumLength(256)
                .WithMessage("MinValue must not exceed 256 characters");
        });

        When(x => x.MaxValue is not null, () =>
        {
            RuleFor(x => x.MaxValue!)
                .MaximumLength(256)
                .WithMessage("MaxValue must not exceed 256 characters");
        });
    }
}
