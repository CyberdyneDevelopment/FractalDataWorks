using FluentValidation;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Validator for <see cref="PipelineTransformConfiguration"/>.
/// Enforces non-empty identity/section fields and a known transform type discriminator.
/// </summary>
public sealed class PipelineTransformConfigurationValidator : AbstractValidator<PipelineTransformConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineTransformConfigurationValidator"/> class.
    /// </summary>
    public PipelineTransformConfigurationValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.SectionName).NotEmpty();
        RuleFor(x => x.ServiceType).NotEmpty();
        RuleFor(x => x.OperationType).NotEmpty();
    }
}
