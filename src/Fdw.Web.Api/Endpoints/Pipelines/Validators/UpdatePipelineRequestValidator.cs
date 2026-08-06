using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Pipelines.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="UpdatePipelineRequest"/>.
/// </summary>
public abstract class UpdatePipelineRequestValidator : FdwEndpointValidator<UpdatePipelineRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePipelineRequestValidator"/> class.
    /// </summary>
    protected UpdatePipelineRequestValidator()
    {
        ValidateName(x => x.Name);
    }
}
