using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Etl.Validators;

/// <summary>
/// Validator for <see cref="EtlPipelineConfiguration"/>.
/// </summary>
public sealed class EtlPipelineConfigurationValidator : FdwConfigurationValidator<EtlPipelineConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EtlPipelineConfigurationValidator"/> class.
    /// </summary>
    public EtlPipelineConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .IsValidName(200);
    }
}
