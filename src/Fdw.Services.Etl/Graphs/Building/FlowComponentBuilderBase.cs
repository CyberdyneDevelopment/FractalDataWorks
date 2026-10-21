using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Base class for the options of <see cref="FlowComponentBuilders"/>, using the CRTP pattern.
/// </summary>
/// <remarks>
/// The settings a component states have already been checked against its kind (every required one is there, none is
/// one the kind does not take), so a builder reads what its kind requires without testing for it. A flow component refuses what
/// it cannot work with by throwing an <see cref="ArgumentException"/> from its constructor; <see cref="Build"/> turns that into
/// a failure that names the component.
/// </remarks>
public abstract class FlowComponentBuilderBase : TypeOptionBase<FlowComponentBuilderBase>, IFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="FlowComponentBuilderBase"/> class.</summary>
    /// <param name="name">The component kind this builder builds.</param>
    protected FlowComponentBuilderBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public IGenericResult<IFlowComponent> Build(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        try
        {
            var created = Create(component, context);
            return created.IsFailure || created.Value is null ? created : MapInputs(component, created.Value, context);
        }
        catch (ArgumentException ex)
        {
            return GenericResult<IFlowComponent>.Failure(
                GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, ex.Message));
        }
    }

    // The component works on the rows its inputs map as they arrive (FDW-2013): each input that states a mapping is wrapped, whatever the kind.
    private static IGenericResult<IFlowComponent> MapInputs(PipelineComponentConfiguration component, IFlowComponent inner, FlowBuildContext context)
    {
        var mapped = component.Fields
            .Where(field => string.Equals(field.SettingName, ComponentTypeBase.MappingsSetting, StringComparison.OrdinalIgnoreCase))
            .Select(field => field.InputName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (mapped.Count == 0)
        {
            return GenericResult<IFlowComponent>.Success(inner);
        }

        var byInput = new Dictionary<string, IReadOnlyList<FlowFieldMap>>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in mapped)
        {
            if (input is null)
            {
                return GenericResult<IFlowComponent>.Failure(
                    GraphLog.ComponentInvalid(context.Logger, component.Name, component.ComponentType, "a mapping names no input"));
            }

            var maps = Maps(component, ComponentTypeBase.MappingsSetting, input, context);
            if (maps.IsFailure || maps.Value is null)
            {
                return maps.ToNewResult<IFlowComponent>();
            }

            byInput[input] = maps.Value;
        }

        return GenericResult<IFlowComponent>.Success(new FlowMappedComponent(inner, byInput, context.Logger));
    }

    /// <summary>Builds the flow component.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The flow component, or the failure that says what the component's settings get wrong.</returns>
    public abstract IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context);

    /// <summary>Reads a list of field mappings of one setting, in order.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="setting">The setting the mappings belong to.</param>
    /// <param name="inputName">The input node the mappings belong to, or <see langword="null"/> when the setting is not per input.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The mappings, or the failure that says which one is incomplete or unreadable.</returns>
    protected static IGenericResult<IReadOnlyList<FlowFieldMap>> Maps(
        PipelineComponentConfiguration component, string setting, string? inputName, FlowBuildContext context)
    {
        var maps = new List<FlowFieldMap>();
        var rows = component.Fields
            .Where(field => string.Equals(field.SettingName, setting, StringComparison.OrdinalIgnoreCase)
                && string.Equals(field.InputName, inputName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(field => field.Ordinal);
        foreach (var row in rows)
        {
            var made = MapOf(component, row, context);
            if (made.IsFailure || made.Value is null)
            {
                return made.ToNewResult<IReadOnlyList<FlowFieldMap>>();
            }

            maps.Add(made.Value);
        }

        return GenericResult<IReadOnlyList<FlowFieldMap>>.Success(maps);
    }

    // One mapping: where its value comes from, and what it does to the value when it states a default, a chain, a type or a required flag (FDW-2000).
    private static IGenericResult<FlowFieldMap> MapOf(PipelineComponentConfiguration component, PipelineComponentFieldConfiguration row, FlowBuildContext context)
    {
        var plain = PlainMap(component, row, context);
        if (plain.IsFailure || plain.Value is null)
        {
            return plain;
        }

        var conversion = ConversionOf(component, row, context);
        if (conversion.IsFailure)
        {
            return conversion.ToNewResult<FlowFieldMap>();
        }

        return conversion.Value is null ? plain : GenericResult<FlowFieldMap>.Success(plain.Value.WithConversion(conversion.Value));
    }

    private static IGenericResult<FlowFieldMap> PlainMap(PipelineComponentConfiguration component, PipelineComponentFieldConfiguration row, FlowBuildContext context)
    {
        if (row.SourceField is not null)
        {
            return GenericResult<FlowFieldMap>.Success(FlowFieldMap.From(row.TargetField, row.SourceField));
        }

        if (string.IsNullOrWhiteSpace(row.ConstantType))
        {
            return GenericResult<FlowFieldMap>.Failure(GraphLog.FieldMappingIncomplete(context.Logger, component.Name, row.TargetField));
        }

        var type = DataTypes.ByName(row.ConstantType);
        if (ReferenceEquals(type, DataTypes.NotFound))
        {
            return GenericResult<FlowFieldMap>.Failure(GraphLog.FieldMappingIncomplete(context.Logger, component.Name, row.TargetField));
        }

        if (row.ConstantText is null)
        {
            return GenericResult<FlowFieldMap>.Success(FlowFieldMap.Of(row.TargetField, null));
        }

        var value = type.Convert(row.ConstantText);
        return value is null || value.IsFailure
            ? GenericResult<FlowFieldMap>.Failure(GraphLog.ConstantUnreadable(context.Logger, component.Name, row.TargetField, row.ConstantText, row.ConstantType))
            : GenericResult<FlowFieldMap>.Success(FlowFieldMap.Of(row.TargetField, value.Value));
    }

    // Null when the mapping states none of the four parts. A type a mapping may not convert to, a default the type cannot read and a chain the run did
    // not resolve are each a failure that names the mapping.
    private static IGenericResult<FlowFieldConversion?> ConversionOf(PipelineComponentConfiguration component, PipelineComponentFieldConfiguration row, FlowBuildContext context)
    {
        if (row.TargetType is null && row.DefaultText is null && !row.IsRequired && row.TransformationChain.Count == 0)
        {
            return GenericResult<FlowFieldConversion?>.Success(null);
        }

        if (row.TargetType is { } targetType && !FieldTypeConversion.IsSupported(targetType))
        {
            return GenericResult<FlowFieldConversion?>.Failure(
                GraphLog.FieldConversionInvalid(context.Logger, component.Name, row.TargetField, $"the type '{targetType}' is not one a mapping converts to"));
        }

        if (row.TargetType is { } type && row.DefaultText is { } text && row.TransformationChain.Count == 0
            && !FieldTypeConversion.TryConvert(text, type, out _, out var unreadable))
        {
            return GenericResult<FlowFieldConversion?>.Failure(
                GraphLog.FieldConversionInvalid(context.Logger, component.Name, row.TargetField, $"the default '{text}' cannot be read as {type}: {unreadable}"));
        }

        IReadOnlyList<IFieldMappingTransformation> chain = [];
        if (row.TransformationChain.Count > 0 && !context.FieldChains.TryGetValue(row.Id, out chain!))
        {
            return GenericResult<FlowFieldConversion?>.Failure(
                GraphLog.FieldConversionInvalid(context.Logger, component.Name, row.TargetField, "its chain of transformations was not resolved for this run"));
        }

        return GenericResult<FlowFieldConversion?>.Success(new FlowFieldConversion(row.TargetType, row.DefaultText, row.IsRequired, chain));
    }

    /// <summary>Reads a list of columns of one setting, in order.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="setting">The setting the columns belong to.</param>
    /// <returns>The column names.</returns>
    protected static IReadOnlyList<string> Columns(PipelineComponentConfiguration component, string setting) =>
        [.. component.Columns
            .Where(column => string.Equals(column.SettingName, setting, StringComparison.OrdinalIgnoreCase))
            .OrderBy(column => column.Ordinal)
            .Select(column => column.ColumnName)];

    /// <summary>Makes the unmatched-rows rule a component's <c>unmatchedOutput</c> setting states.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>An unmatched row goes to the named output, or fails the run when none is named.</returns>
    protected static FlowUnmatched Unmatched(PipelineComponentConfiguration component) =>
        component.UnmatchedOutput is { } output ? FlowUnmatched.SendTo(output) : FlowUnmatched.Fail;
}
