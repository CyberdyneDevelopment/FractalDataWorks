using System;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// The value of one declared setting of a stored component, as its builder gets it (FDW-6133): the stored text already read as the setting's data type,
/// or, for a setting read when the step runs, text that names variables as <c>{{Name}}</c>, rendered by <see cref="VariableText"/> from the variables as
/// the step finds them and then read as the data type. The counterpart of a column setting read with <see cref="ScalarSetting"/>.
/// </summary>
public sealed class ComponentSettingValue
{
    private readonly Func<FlowVariables, ILogger, IGenericResult<object>> _read;

    internal ComponentSettingValue(string name, object stated)
    {
        Name = name;
        _read = (_, _) => GenericResult<object>.Success(stated);
    }

    internal ComponentSettingValue(string component, string name, IDataType type, string text)
    {
        Name = name;
        ReadsWhenStepRuns = true;
        _read = (variables, logger) => Rendered(variables, logger, component, name, type, text);
    }

    /// <summary>Gets the setting's name, as the component type declares it.</summary>
    public string Name { get; }

    /// <summary>Gets a value indicating whether the value names variables and is read when the step runs.</summary>
    public bool ReadsWhenStepRuns { get; }

    /// <summary>Reads the value as its data type: the stored one, or the stored text rendered from the variables the step can see.</summary>
    /// <param name="variables">The variables the step can see when it runs.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The value, or ComponentSettingVariableMissing or ComponentSettingRenderedUnreadable.</returns>
    public IGenericResult<object> Read(FlowVariables variables, ILogger logger) => _read(variables, logger);

    // Why the exception is caught: VariableText throws for a variable not in scope or a parameter the run left blank, and every step that renders text turns it into a coded failure.
    private static IGenericResult<object> Rendered(FlowVariables variables, ILogger logger, string component, string name, IDataType type, string text)
    {
        object? value;
        try
        {
            value = VariableText.Typed(text)(variables);
        }
        catch (InvalidOperationException exception)
        {
            return GenericResult<object>.Failure(GraphLog.ComponentSettingVariableMissing(logger, exception, component, name, text, exception.Message));
        }

        var read = type.Convert(value);
        return read.IsSuccess && read.Value is { } converted
            ? GenericResult<object>.Success(converted)
            : GenericResult<object>.Failure(GraphLog.ComponentSettingRenderedUnreadable(logger, component, name, text, type.Name));
    }
}
