using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="EnvironmentApproverConfiguration"/>.
/// </summary>
public sealed class EnvironmentApproverConfigurationValidator : FdwConfigurationValidator<EnvironmentApproverConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentApproverConfigurationValidator"/> class.
    /// </summary>
    public EnvironmentApproverConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.ApproverName)
            .NotEmpty()
            .WithMessage("ApproverName is required");

        RuleFor(x => x.ApprovalOrder)
            .GreaterThanOrEqualTo(1)
            .WithMessage("ApprovalOrder must be at least 1");
    }
}
