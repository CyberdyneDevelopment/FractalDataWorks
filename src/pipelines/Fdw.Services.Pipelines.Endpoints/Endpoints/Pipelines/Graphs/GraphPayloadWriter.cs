using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Tools;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Writes a stored graph the way the API states one (FDW-1396): the same shape the save reads, so a read after a save answers
/// with what was saved.
/// </summary>
public static class GraphPayloadWriter
{
    /// <summary>Writes a stored graph.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>The payload.</returns>
    public static GraphPayload Write(GraphPipelineConfiguration graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return new GraphPayload
        {
            Name = graph.Name,
            Description = graph.Description,
            IsEnabled = graph.IsEnabled,
            LinkCapacity = graph.LinkCapacity,
            RequireApprovalToRun = graph.RequireApprovalToRun,
            MaxParallelRuns = graph.MaxParallelRuns,
            OverflowBehavior = graph.OverflowBehavior,
            DelayedStartSeconds = graph.DelayedStartSeconds,
            DelayedStartTimeOfDay = graph.DelayedStartTimeOfDay,
            DelayedStartTimeZoneId = graph.DelayedStartTimeZoneId,
            Variables = [.. graph.Variables.Select(variable => new GraphVariablePayload
            {
                Name = variable.Name, DataType = variable.DataType, Value = variable.ValueText,
            })],
            Parameters = [.. graph.Parameters.Select(parameter => new GraphParameterPayload
            {
                Name = parameter.Name, DataType = parameter.ParameterType, IsRequired = parameter.IsRequired, IsReadOnly = parameter.IsReadOnly,
                IsList = parameter.IsList, ShowsValue = parameter.ShowsValue, DefaultValue = parameter.DefaultText, Description = parameter.Description,
            })],
            Components = [.. graph.Components.Select(WriteComponent)],
            Links =
            [
                .. graph.Links.Select(link => new GraphLinkPayload
                {
                    From = new GraphNodePayload { Component = link.FromComponent, Node = link.FromOutput },
                    To = new GraphNodePayload { Component = link.ToComponent, Node = link.ToInput },
                }),
            ],
            Dropped = [.. graph.DroppedOutputs.Select(output => new GraphNodePayload { Component = output.ComponentName, Node = output.OutputName })],
        };
    }

    private static GraphComponentPayload WriteComponent(PipelineComponentConfiguration component)
    {
        var settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var scalar in ComponentSettingBindings.Scalars)
        {
            if (scalar.Get(component) is { } value)
            {
                settings[scalar.Name] = value;
            }
        }

        foreach (var binding in component.SettingBindings)
        {
            settings[binding.SettingName] = GraphSettingValue.Bound(binding.ParameterName);
        }

        foreach (var group in component.Columns.GroupBy(column => column.SettingName, StringComparer.OrdinalIgnoreCase))
        {
            settings[group.Key] = group.OrderBy(column => column.Ordinal).Select(column => column.ColumnName).ToList();
        }

        foreach (var group in component.Fields.GroupBy(field => field.SettingName, StringComparer.OrdinalIgnoreCase))
        {
            settings[group.Key] = group.OrderBy(field => field.Ordinal).Select(WriteFieldMap).ToList();
        }

        if (component.SplitOutputs.Count > 0)
        {
            settings["outputs"] = component.SplitOutputs.OrderBy(output => output.Ordinal)
                .Select(output => new GraphSplitOutputPayload { Name = output.OutputName, Filter = output.FilterExpression }).ToList();
        }

        if (component.ValueSources.Count > 0)
        {
            settings["valueSources"] = component.ValueSources.OrderBy(source => source.Ordinal).Select(WriteValueSource).ToList();
        }

        if (component.Parameters.Count > 0)
        {
            settings["parameters"] = component.Parameters
                .Select(parameter => new GraphComponentParameterPayload { Name = parameter.ParameterName, Value = parameter.ValueText }).ToList();
        }

        if (component.Stages.Count > 0 && ComponentTypes.ByName(component.ComponentType) is StageToolComponentTypeBase tool)
        {
            WriteToolMembers(settings, tool, component.Stages[0], component.SettingBindings);
        }

        return new GraphComponentPayload
        {
            Name = component.Name,
            Kind = component.ComponentType,
            Parent = component.ParentComponent,
            Settings = settings,
            Layout = component.LayoutX is { } x && component.LayoutY is { } y ? new GraphLayoutPayload { X = x, Y = y } : null,
        };
    }

    private static PipelineLoadColumnSource WriteValueSource(PipelineComponentValueSourceConfiguration source) => new()
    {
        Column = source.ColumnName,
        Source = new PipelineLoadValueSource
        {
            Kind = source.SourceKind,
            SourceField = source.SourceField,
            SequenceName = source.SequenceName,
            Schema = source.SequenceSchema,
            Database = source.SequenceDatabase,
            Value = source.ConstantValue,
            ValueType = source.ConstantDataType,
            OrderBy = [.. source.OrderColumns.OrderBy(column => column.Ordinal).Select(column => column.ColumnName)],
        },
    };

    // A tool's settings are the members of its stage, stated as the stage states them, so what is written back is what was read.
    private static void WriteToolMembers(
        Dictionary<string, object?> settings,
        StageToolComponentTypeBase tool,
        PipelineTransformConfiguration stage,
        IList<PipelineComponentSettingBindingConfiguration> bindings)
    {
        foreach (var (member, value) in ToolSettings.Members(tool, PipelineDetailMapper.MapTransformToDto(stage, new Dictionary<Guid, string>())))
        {
            // A member whose list a run supplies is written as the binding that was already stated, not as the empty list the stage holds.
            if (!bindings.Any(binding => string.Equals(binding.SettingName, member, StringComparison.OrdinalIgnoreCase)))
            {
                settings[member] = value;
            }
        }
    }

    private static GraphFieldMapPayload WriteFieldMap(PipelineComponentFieldConfiguration field) => new()
    {
        Input = field.InputName,
        Target = field.TargetField,
        Source = field.SourceField,
        Constant = field.ConstantType is { } type ? new GraphConstantPayload { Type = type, Value = field.ConstantText } : null,
        TargetType = field.TargetType,
        DefaultValue = field.DefaultText,
        IsRequired = field.IsRequired ? true : null,
        Transformations = field.TransformationChain.Count == 0
            ? null
            : [.. field.TransformationChain.OrderBy(step => step.Ordinal)
                .Select(step => new PipelineFieldTransformationReference { TransformationId = step.TransformationId, Ordinal = step.Ordinal })],
    };
}
