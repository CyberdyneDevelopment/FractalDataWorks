using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Pipelines.Validators;

/// <summary>
/// Validator for <see cref="PipelineConfiguration"/>.
/// </summary>
public sealed class PipelineConfigurationValidator : FdwConfigurationValidator<PipelineConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineConfigurationValidator"/> class.
    /// </summary>
    public PipelineConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
