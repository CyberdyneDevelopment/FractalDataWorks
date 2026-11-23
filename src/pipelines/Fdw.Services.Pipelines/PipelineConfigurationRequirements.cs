using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>
/// What a pipeline type requires of its configuration, checked by the layer for every type (FDW-6141).
/// </summary>
public static class PipelineConfigurationRequirements
{
    /// <summary>Finds the requirements a type's configuration cannot state because it does not implement the interface that carries the fact.</summary>
    /// <param name="type">The pipeline type.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>One message for each such requirement, naming the type and the fact; empty when every one can be stated.</returns>
    public static IReadOnlyList<IGenericMessage> Gaps(IPipelineType type, ILogger logger)
        => [.. type.ConfigurationRequirements
            .Where(requirement => !requirement.ConfigurationInterface.IsAssignableFrom(type.ConfigurationType))
            .Select(requirement => PipelineTypesLog.ConfigurationCannotStateRequirement(logger, type.Name, requirement.FactName, requirement.ConfigurationInterface.Name))];

    /// <summary>Finds the requirements a row of the type leaves out.</summary>
    /// <param name="type">The pipeline type.</param>
    /// <param name="configuration">The implementation row.</param>
    /// <returns>The requirements the row does not state.</returns>
    public static IReadOnlyList<PipelineConfigurationRequirement> Unstated(IPipelineType type, object configuration)
        => [.. type.ConfigurationRequirements.Where(requirement => !requirement.IsStatedBy(configuration))];

    /// <summary>Refuses a row that leaves out a fact its type requires.</summary>
    /// <param name="type">The pipeline type.</param>
    /// <param name="configuration">The implementation row.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>One message for each fact left out, naming the type and the fact; empty when the row states every one.</returns>
    public static IReadOnlyList<IGenericMessage> Missing(IPipelineType type, object configuration, ILogger logger)
        => [.. Unstated(type, configuration).Select(requirement => PipelineTypesLog.RequiredFactNotStated(logger, type.Name, requirement.FactName))];
}
