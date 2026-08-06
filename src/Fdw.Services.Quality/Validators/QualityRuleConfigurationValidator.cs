using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="QualityRuleConfiguration"/>.
/// </summary>
public sealed class QualityRuleConfigurationValidator : FdwConfigurationValidator<QualityRuleConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityRuleConfigurationValidator"/> class.
    /// </summary>
    public QualityRuleConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.DataSetName)
            .NotEmpty()
            .WithMessage("DataSetName is required");
    }
}
