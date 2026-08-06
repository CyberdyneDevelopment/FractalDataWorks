using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="PromotionRequestConfiguration"/>.
/// </summary>
public sealed class PromotionRequestConfigurationValidator : FdwConfigurationValidator<PromotionRequestConfiguration>
{
    private static readonly string[] ValidStatuses = ["Pending", "Approved", "Rejected", "Completed", "Cancelled"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionRequestConfigurationValidator"/> class.
    /// </summary>
    public PromotionRequestConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.SourceEnvironment)
            .NotEmpty()
            .WithMessage("SourceEnvironment is required");

        RuleFor(x => x.TargetEnvironment)
            .NotEmpty()
            .WithMessage("TargetEnvironment is required");

        RuleFor(x => x.RequestedBy)
            .NotEmpty()
            .WithMessage("RequestedBy is required");

        RuleFor(x => x.Status)
            .Must(s => System.Array.IndexOf(ValidStatuses, s) >= 0)
            .WithMessage("Status must be one of: Pending, Approved, Rejected, Completed, Cancelled");
    }
}
