using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Pipelines.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreatePipelineRequest"/>.
/// </summary>
public abstract class CreatePipelineRequestValidator : FdwEndpointValidator<CreatePipelineRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePipelineRequestValidator"/> class.
    /// </summary>
    protected CreatePipelineRequestValidator()
    {
        ValidateName(x => x.Name, maxLength: 100);

        RuleFor(x => x.PipelineType)
            .NotEmpty()
            .WithMessage("PipelineType is required");

        RuleFor(x => x.SourceConnectionName)
            .NotEmpty()
            .WithMessage("SourceConnectionName is required");

        RuleFor(x => x.DestinationConnectionName)
            .NotEmpty()
            .WithMessage("DestinationConnectionName is required");

        // Why: transforms are optional, but any supplied transform must be fully specified — a transform
        // with a missing OperationType or a field mapping with missing source/destination would silently
        // drop data at runtime, so validate the whole submitted hierarchy up front rather than fail-loud
        // deep in the cascade-save.
        RuleForEach(x => x.Transforms).ChildRules(transform =>
        {
            transform.RuleFor(t => t.Name)
                .NotEmpty()
                .WithMessage("Transform Name is required")
                .MaximumLength(256);

            transform.RuleFor(t => t.OperationType)
                .NotEmpty()
                .WithMessage("Transform OperationType is required");

            transform.RuleForEach(t => t.FieldMappings).ChildRules(mapping =>
            {
                mapping.RuleFor(m => m.Name)
                    .NotEmpty()
                    .WithMessage("Field mapping Name is required")
                    .MaximumLength(256);

                mapping.RuleFor(m => m.SourceField)
                    .NotEmpty()
                    .WithMessage("Field mapping SourceField is required")
                    .MaximumLength(256);

                mapping.RuleFor(m => m.DestinationField)
                    .NotEmpty()
                    .WithMessage("Field mapping DestinationField is required")
                    .MaximumLength(256);
            });
        });
    }
}
