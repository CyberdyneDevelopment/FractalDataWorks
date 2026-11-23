using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The pipeline an Execute pipeline step starts (FDW-6135): one its author named, or one a variable names when the step runs, which must be on the list the step states.
/// </summary>
public sealed class FlowPipelineTarget
{
    private readonly string? _named;
    private readonly string? _variable;
    private readonly IReadOnlyList<string> _targets;

    private FlowPipelineTarget(string? named, string? variable, IReadOnlyList<string> targets, string label)
    {
        _named = named;
        _variable = variable;
        _targets = targets;
        Label = label;
    }

    /// <summary>Gets a value indicating whether a variable names the pipeline, so that the start must be offered to the caller.</summary>
    public bool NamedByVariable => _variable is not null;

    /// <summary>Gets what the step's messages call its target before it is resolved: the pipeline, or the variable.</summary>
    public string Label { get; }

    /// <summary>Makes the target of a step whose author named the pipeline.</summary>
    /// <param name="pipeline">The pipeline.</param>
    /// <returns>The target.</returns>
    public static FlowPipelineTarget Named(string pipeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeline);
        return new FlowPipelineTarget(pipeline, null, [], pipeline);
    }

    /// <summary>Makes the target of a step that starts the pipeline a variable names.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="targets">The pipelines the step may start; at least one.</param>
    /// <returns>The target.</returns>
    public static FlowPipelineTarget FromVariable(string variable, IReadOnlyList<string> targets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        if (targets.Count == 0)
        {
            throw new ArgumentException("A step that starts the pipeline a variable names states the pipelines it may start.", nameof(targets));
        }

        return new FlowPipelineTarget(null, variable, targets, $"the pipeline named by the variable '{variable}'");
    }

    /// <summary>Resolves the pipeline to start, now.</summary>
    /// <param name="variables">The variables in scope when the step runs.</param>
    /// <param name="step">The step's name.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The pipeline's name, or the refusal: no value, or a name that is not on the list.</returns>
    public IGenericResult<string> Resolve(FlowVariables variables, string step, ILogger logger)
    {
        if (_named is not null)
        {
            return GenericResult<string>.Success(_named);
        }

        if (!variables.TryGet(_variable!, out var value) || value is null || string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)))
        {
            return GenericResult<string>.Failure(FlowLog.ExecutePipelineTargetVariableMissing(logger, step, _variable!));
        }

        var name = Convert.ToString(value, CultureInfo.InvariantCulture)!.Trim();
        var listed = _targets.FirstOrDefault(target => string.Equals(target, name, StringComparison.OrdinalIgnoreCase));
        return listed is null
            ? GenericResult<string>.Failure(FlowLog.ExecutePipelineTargetNotListed(logger, step, name, string.Join(", ", _targets)))
            : GenericResult<string>.Success(listed);
    }
}
