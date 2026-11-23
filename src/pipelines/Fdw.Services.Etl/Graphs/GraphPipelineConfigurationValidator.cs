using FluentValidation;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Validation;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Validator for <see cref="GraphPipelineConfiguration"/> (FDW-6141): a row is checked against what its pipeline type requires of it, whichever graph pipeline type it is.
/// </summary>
/// <remarks>
/// The graph's own shape is judged where it always was (<see cref="GraphShapeValidator"/>); this states what the TYPE requires. A type that requires nothing (Graph) accepts any
/// row. An implementation no type names is refused elsewhere, so it is not judged here.
/// </remarks>
public sealed class GraphPipelineConfigurationValidator : FdwConfigurationValidator<GraphPipelineConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphPipelineConfigurationValidator"/> class.</summary>
    public GraphPipelineConfigurationValidator()
    {
        RuleFor(row => row).Custom((row, context) =>
        {
            var type = PipelineTypes.ByName(row.Implementation);
            if (ReferenceEquals(type, PipelineTypes.NotFound))
                return;

            foreach (var requirement in PipelineConfigurationRequirements.Unstated(type, row))
                context.AddFailure(requirement.FactName, $"A pipeline of type {type.Name} must state {requirement.FactName}");
        });
    }
}
