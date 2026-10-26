using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The Built kind: the fields a calculation reads and what it produces. It produces either <c>results</c>, each a formula whose
/// answer is a column of the record, or <c>tableOutputs</c>, each a table of rows made from the <c>tableInputs</c> and
/// <c>derived</c> values. It refuses the members of the Field kind, and a transformation does not state both kinds of output.
/// </summary>
/// <remarks>
/// A definition is checked by the calculation engine against the platform's limit settings before anything is stored, so a
/// definition that saves is one a pipeline run can compile. It is stored as rows, not as text.
/// </remarks>
[TypeOption(typeof(TransformationKinds), "Built", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BuiltTransformationKind : TransformationKindBase
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationKind"/> class.</summary>
    public BuiltTransformationKind() : base("Built")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<ITransformationImplementationConfiguration> Prepare(
        CreateTransformationRequest request, TransformationKindContext context)
    {
        var refusal = FieldMemberRefusal(request.TransformationType, request.Parameters, context);
        if (refusal is not null)
        {
            return Refused(refusal);
        }

        if (request.Inputs is null)
        {
            return Refused(TransformationKindLog.MemberRequired(context.Logger, Name, "inputs"));
        }

        if (request.TableOutputs is { Count: > 0 })
        {
            return BuiltSet(request.Name, request.Description, request.Inputs, request.Results, request.TableInputs ?? [], request.Derived ?? [], request.TableOutputs, context);
        }

        if (request.Results is null)
        {
            return Refused(TransformationKindLog.MemberRequired(context.Logger, Name, "results"));
        }

        var tableMember = TableMemberRefusal(request.Name, request.TableInputs, request.Derived, context);
        return tableMember is not null ? Refused(tableMember) : Built(request.Name, request.Description, request.Inputs, request.Results, context);
    }

    /// <inheritdoc />
    public override IGenericResult<ITransformationImplementationConfiguration> PrepareUpdate(
        UpdateTransformationRequest request, ITransformationImplementationConfiguration existing, TransformationKindContext context)
    {
        var refusal = FieldMemberRefusal(request.TransformationType, request.Parameters, context);
        if (refusal is not null)
        {
            return Refused(refusal);
        }

        if (existing is not BuiltTransformationConfiguration stored)
        {
            return Refused(BuiltTransformationEndpointLog.NotBuilt(context.Logger, existing.Name, existing.Implementation));
        }

        var prepared = request.TableOutputs is { Count: > 0 } || (request.Results is null && stored.TableOutputs.Count > 0)
            ? UpdateSet(request, stored, context)
            : UpdateRecord(request, stored, context);
        if (prepared.IsSuccess)
        {
            // The new definition takes the stored one's Id, so the same transformation is replaced and not added.
            prepared.Value!.Id = stored.Id;
        }

        return prepared;
    }

    /// <inheritdoc />
    public override void Describe(ITransformationImplementationConfiguration transformation, TransformationPayload payload)
    {
        if (transformation is not BuiltTransformationConfiguration built)
        {
            return;
        }

        payload.Inputs = [.. built.Inputs.OrderBy(i => i.ExecutionOrder).Select(i => new TransformationInputPayload { Name = i.Name, ValueType = i.ValueType })];
        payload.Outputs = [.. built.Outputs.OrderBy(o => o.ExecutionOrder).Select(o => new TransformationOutputPayload { Name = o.Name, IsKept = o.IsKept })];
    }

    /// <inheritdoc />
    /// <remarks>
    /// A record calculation's results are typed by the calculation engine from the stored formulas. A definition the engine refuses
    /// leaves its results untyped and is logged, so one bad definition does not fail a list of many. A set's outputs are tables and
    /// have no value type.
    /// </remarks>
    public override void AddValueTypes(
        ITransformationImplementationConfiguration transformation, TransformationPayload payload, TransformationKindContext context)
    {
        if (transformation is not BuiltTransformationConfiguration { TableOutputs.Count: 0 } built)
        {
            return;
        }

        var kinds = StoredResultKinds(built, context);
        if (kinds.IsFailure)
        {
            BuiltTransformationEndpointLog.ValueTypesUnknown(context.Logger, built.Name);
            return;
        }

        foreach (var output in payload.Outputs)
        {
            if (kinds.Value!.TryGetValue(output.Name, out var kind))
            {
                output.ValueType = kind.Name;
                output.DataType = kind.DataTypeName;
            }
        }
    }

    /// <inheritdoc />
    public override IGenericResult Detail(
        ITransformationImplementationConfiguration transformation, TransformationPayload payload, TransformationKindContext context)
    {
        Describe(transformation, payload);
        AddValueTypes(transformation, payload, context);
        if (transformation is not BuiltTransformationConfiguration built)
        {
            return GenericResult.Failure(BuiltTransformationEndpointLog.NotBuilt(context.Logger, transformation.Name, transformation.Implementation));
        }

        if (built.TableOutputs.Count > 0)
        {
            return DetailSet(built, payload, context);
        }

        var results = StoredResults(built, context);
        if (results.IsFailure)
        {
            return results;
        }

        payload.Results = [.. results.Value!];
        return GenericResult.Success();
    }

    private static IGenericResult DetailSet(BuiltTransformationConfiguration built, TransformationPayload payload, TransformationKindContext context)
    {
        var set = StoredSet(built, context);
        if (set.IsFailure)
        {
            return set;
        }

        payload.TableInputs = [.. set.Value!.TableInputs.Select(item => BuiltSetPayloads.Of(item))];
        payload.Derived = [.. set.Value.Derived.Select(item => BuiltSetPayloads.Of(item))];
        payload.TableOutputs = [.. set.Value.Outputs.Select(item => BuiltSetPayloads.Of(item))];
        return GenericResult.Success();
    }

    private static IGenericResult<ITransformationImplementationConfiguration> UpdateRecord(
        UpdateTransformationRequest request, BuiltTransformationConfiguration stored, TransformationKindContext context)
    {
        var tableMember = TableMemberRefusal(stored.Name, request.TableInputs, request.Derived, context);
        if (tableMember is not null)
        {
            return Refused(tableMember);
        }

        var results = request.Results is not null ? GenericResult<IList<BuiltResultPayload>>.Success(request.Results) : StoredResults(stored, context);
        return results.IsFailure
            ? results.ToNewResult<ITransformationImplementationConfiguration>()
            : Built(stored.Name, request.Description ?? stored.Description, request.Inputs ?? StoredInputs(stored), results.Value!, context);
    }

    private static IGenericResult<ITransformationImplementationConfiguration> UpdateSet(
        UpdateTransformationRequest request, BuiltTransformationConfiguration stored, TransformationKindContext context)
    {
        if (request.Results is { Count: > 0 })
        {
            return Refused(BuiltTransformationEndpointLog.ResultsAndTableOutputs(context.Logger, stored.Name));
        }

        var current = StoredSet(stored, context);
        if (current.IsFailure)
        {
            return current.ToNewResult<ITransformationImplementationConfiguration>();
        }

        var set = current.Value!;
        var tables = request.TableInputs ?? [.. set.TableInputs.Select(item => BuiltSetPayloads.Of(item))];
        var derived = request.Derived ?? [.. set.Derived.Select(item => BuiltSetPayloads.Of(item))];
        var outputs = request.TableOutputs ?? [.. set.Outputs.Select(item => BuiltSetPayloads.Of(item))];
        return BuiltSet(stored.Name, request.Description ?? stored.Description, request.Inputs ?? StoredInputs(stored), null, tables, derived, outputs, context);
    }

    private static IGenericResult<ITransformationImplementationConfiguration> BuiltSet(
        string name,
        string? description,
        IList<TransformationInputPayload> inputs,
        IList<BuiltResultPayload>? results,
        IList<TableInputPayload> tables,
        IList<DerivedValuePayload> derived,
        IList<TableOutputPayload> outputs,
        TransformationKindContext context)
    {
        if (results is { Count: > 0 })
        {
            return Refused(BuiltTransformationEndpointLog.ResultsAndTableOutputs(context.Logger, name));
        }

        var prepared = BuiltSetSupport.Prepare(context.Settings, name, description, inputs, tables, derived, outputs, context.Logger);
        return prepared.IsFailure
            ? prepared.ToNewResult<ITransformationImplementationConfiguration>()
            : GenericResult<ITransformationImplementationConfiguration>.Success(prepared.Value!);
    }

    private IGenericMessage? FieldMemberRefusal(string? transformationType, IList<TransformationParameterPayload>? parameters, TransformationKindContext context)
    {
        if (!string.IsNullOrWhiteSpace(transformationType))
        {
            return TransformationKindLog.MemberNotForKind(context.Logger, Name, "transformationType");
        }

        return parameters is { Count: > 0 }
            ? TransformationKindLog.MemberNotForKind(context.Logger, Name, "parameters")
            : null;
    }

    // Table inputs and derived values only mean something to a transformation that writes tables.
    private static IGenericMessage? TableMemberRefusal(string name, IList<TableInputPayload>? tables, IList<DerivedValuePayload>? derived, TransformationKindContext context)
    {
        if (tables is { Count: > 0 })
        {
            return BuiltTransformationEndpointLog.TableMemberWithoutTableOutputs(context.Logger, name, "tableInputs");
        }

        return derived is { Count: > 0 }
            ? BuiltTransformationEndpointLog.TableMemberWithoutTableOutputs(context.Logger, name, "derived")
            : null;
    }

    private static IGenericResult<ITransformationImplementationConfiguration> Built(
        string name, string? description, IList<TransformationInputPayload> inputs, IList<BuiltResultPayload> results, TransformationKindContext context)
    {
        var prepared = BuiltTransformationSupport.Prepare(context.Settings, name, description, inputs, results, context.Logger);
        return prepared.IsFailure
            ? prepared.ToNewResult<ITransformationImplementationConfiguration>()
            : GenericResult<ITransformationImplementationConfiguration>.Success(prepared.Value!);
    }

    private static IList<TransformationInputPayload> StoredInputs(BuiltTransformationConfiguration stored) =>
        [.. stored.Inputs.OrderBy(i => i.ExecutionOrder).Select(i => new TransformationInputPayload { Name = i.Name, ValueType = i.ValueType })];

    // The stored set is read from its rows, which are refused when they do not form a set; they are never repaired.
    private static IGenericResult<Fdw.Services.Calculations.Abstractions.Sets.CalculationSetDefinition> StoredSet(
        BuiltTransformationConfiguration stored, TransformationKindContext context)
    {
        var limits = CalculationLimitSettings.Read(context.Settings, context.Logger);
        return limits.IsFailure
            ? limits.ToNewResult<Fdw.Services.Calculations.Abstractions.Sets.CalculationSetDefinition>()
            : BuiltTransformationSetReader.ToDefinition(stored, limits.Value!);
    }

    // The stored formulas are read from their rows by the converter, which refuses rows that do not form one tree per result.
    private static IGenericResult<IList<BuiltResultPayload>> StoredResults(BuiltTransformationConfiguration stored, TransformationKindContext context)
    {
        var limits = CalculationLimitSettings.Read(context.Settings, context.Logger);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<IList<BuiltResultPayload>>();
        }

        var array = BuiltTransformationConverter.ToArray(stored, limits.Value!);
        return array.IsFailure
            ? array.ToNewResult<IList<BuiltResultPayload>>()
            : GenericResult<IList<BuiltResultPayload>>.Success(
                [.. array.Value!.Results.Select(result => new BuiltResultPayload { Name = result.Name, IsKept = result.Emit, Formula = BuiltFormulaReader.ToPayload(result.Expression) })]);
    }

    private static IGenericResult<IReadOnlyDictionary<string, ICalculationValueKind>> StoredResultKinds(
        BuiltTransformationConfiguration stored, TransformationKindContext context)
    {
        var limits = CalculationLimitSettings.Read(context.Settings, context.Logger);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<IReadOnlyDictionary<string, ICalculationValueKind>>();
        }

        var array = BuiltTransformationConverter.ToArray(stored, limits.Value!);
        return array.IsFailure
            ? array.ToNewResult<IReadOnlyDictionary<string, ICalculationValueKind>>()
            : CalculationArrayValidator.ResultKinds(array.Value!, limits.Value!);
    }

    private static IGenericResult<ITransformationImplementationConfiguration> Refused(IGenericMessage message) =>
        GenericResult<ITransformationImplementationConfiguration>.Failure(message);
}
