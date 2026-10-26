using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// Replaces the calculation references in a filter tree with the literal values the calculations yield, so the translator reads a
/// tree of literals and checks each against its field as it always has.
/// </summary>
/// <remarks>
/// <para>
/// Why before translation: the translator is synchronous and sees one condition at a time. The point that sees the whole filter
/// before the query runs is here, so every route that translates a filter resolves it first and the translator never meets a
/// reference.
/// </para>
/// <para>
/// A reference is resolved wherever a literal stands: a condition's value, the ends of a date range, the bounds of a Between. The
/// result is checked against the field's type, and every failure names the field and the calculation and refuses the query.
/// Nothing is dropped and nothing is defaulted. The tree that was given is not changed, so a view's stored filter keeps the
/// calculation's name and inputs.
/// </para>
/// </remarks>
internal sealed class FilterCalculationResolver
{
    private readonly IReadOnlyDictionary<string, QueryableField> _fields;
    private readonly IQueryRefusals _refusals;
    private readonly IFilterCalculations? _calculations;

    public FilterCalculationResolver(
        IReadOnlyDictionary<string, QueryableField> fields, IQueryRefusals refusals, IFilterCalculations? calculations)
    {
        _fields = fields;
        _refusals = refusals;
        _calculations = calculations;
    }

    public async Task<IGenericResult<QueryFilterNode?>> Resolve(QueryFilterNode? node, CancellationToken cancellationToken)
    {
        if (node is null)
        {
            return GenericResult<QueryFilterNode?>.Success(null);
        }

        var resolved = await Node(node, cancellationToken).ConfigureAwait(false);
        return resolved.IsFailure
            ? resolved.ToNewResult<QueryFilterNode?>()
            : GenericResult<QueryFilterNode?>.Success(resolved.Value);
    }

    private async Task<IGenericResult<QueryFilterNode>> Node(QueryFilterNode node, CancellationToken cancellationToken)
    {
        if (node.Nodes is not null)
        {
            return await Group(node, node.Nodes, cancellationToken).ConfigureAwait(false);
        }

        // A condition on a field the resource does not declare is the translator's to refuse, with its own words.
        if (node.Field is null || !_fields.TryGetValue(node.Field, out var field))
        {
            return GenericResult<QueryFilterNode>.Success(node);
        }

        var value = await Value(node.Value, field, cancellationToken).ConfigureAwait(false);
        if (value.IsFailure)
        {
            return value.ToNewResult<QueryFilterNode>();
        }

        var start = await Slot(node.StartDate, field, cancellationToken).ConfigureAwait(false);
        if (start.IsFailure)
        {
            return start.ToNewResult<QueryFilterNode>();
        }

        var end = await Slot(node.EndDate, field, cancellationToken).ConfigureAwait(false);
        return end.IsFailure
            ? end.ToNewResult<QueryFilterNode>()
            : GenericResult<QueryFilterNode>.Success(new QueryFilterNode
            {
                Field = node.Field,
                Operator = node.Operator,
                Value = value.Value,
                StartDate = start.Value,
                EndDate = end.Value,
            });
    }

    private async Task<IGenericResult<QueryFilterNode>> Group(
        QueryFilterNode node, IReadOnlyList<QueryFilterNode?> children, CancellationToken cancellationToken)
    {
        var resolved = new List<QueryFilterNode?>(children.Count);
        foreach (var child in children)
        {
            var next = child is null ? GenericResult<QueryFilterNode?>.Success(null) : await Resolve(child, cancellationToken).ConfigureAwait(false);
            if (next.IsFailure)
            {
                return next.ToNewResult<QueryFilterNode>();
            }

            resolved.Add(next.Value);
        }

        return GenericResult<QueryFilterNode>.Success(new QueryFilterNode { Logic = node.Logic, Nodes = resolved });
    }

    // A condition's value: a reference itself, a Between whose bounds may be references, or a literal that stays as it is.
    private async Task<IGenericResult<JsonElement?>> Value(JsonElement? value, QueryableField field, CancellationToken cancellationToken)
    {
        if (value is not { } given || FilterCalculationReference.IsReference(given) || !IsObject(given))
        {
            return await Slot(value, field, cancellationToken).ConfigureAwait(false);
        }

        return await Bounds(given, field, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<JsonElement?>> Bounds(JsonElement bounds, QueryableField field, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in bounds.EnumerateObject())
            {
                writer.WritePropertyName(property.Name);
                var bound = await Slot(property.Value, field, cancellationToken).ConfigureAwait(false);
                if (bound.IsFailure)
                {
                    return bound;
                }

                bound.Value!.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return GenericResult<JsonElement?>.Success(document.RootElement.Clone());
    }

    // One value position: a reference is evaluated and replaced by its literal; anything else is returned as it was.
    private async Task<IGenericResult<JsonElement?>> Slot(JsonElement? value, QueryableField field, CancellationToken cancellationToken)
    {
        if (value is not { } given || !FilterCalculationReference.IsReference(given))
        {
            return GenericResult<JsonElement?>.Success(value);
        }

        var reference = FilterCalculationReference.Read(given, field.Name, _refusals);
        if (reference.IsFailure)
        {
            return reference.ToNewResult<JsonElement?>();
        }

        var evaluated = await Evaluate(reference.Value!, field, cancellationToken).ConfigureAwait(false);
        return evaluated.IsFailure
            ? evaluated.ToNewResult<JsonElement?>()
            : GenericResult<JsonElement?>.Success(FilterCalculationLiterals.Literal(evaluated.Value!));
    }

    private async Task<IGenericResult<FilterCalculationValue>> Evaluate(
        FilterCalculationReference reference, QueryableField field, CancellationToken cancellationToken)
    {
        if (_calculations is null)
        {
            return _refusals.Invalid<FilterCalculationValue>(
                $"the filter on '{field.Name}' names calculation '{reference.Name}', but this host has no calculations to evaluate");
        }

        var inputs = new Dictionary<string, object?>(reference.Inputs.Count, StringComparer.Ordinal);
        foreach (var input in reference.Inputs)
        {
            var read = FilterCalculationLiterals.Input(input.Value, input.Key, reference.Name, _refusals);
            if (read.IsFailure)
            {
                return read.ToNewResult<FilterCalculationValue>();
            }

            inputs[input.Key] = read.Value;
        }

        var evaluated = await _calculations.Evaluate(reference.Name, inputs, cancellationToken).ConfigureAwait(false);
        if (evaluated.IsFailure)
        {
            return _refusals.Invalid<FilterCalculationValue>(
                $"calculation '{reference.Name}' for '{field.Name}' could not be evaluated: {evaluated.CurrentMessage}");
        }

        var checkedType = FilterCalculationLiterals.Check(evaluated.Value!, reference.Name, field, _refusals);
        return checkedType.IsFailure ? checkedType.ToNewResult<FilterCalculationValue>() : evaluated;
    }

    private static bool IsObject(JsonElement element)
    {
#pragma warning disable FDW018 // External System.Text.Json JsonValueKind enum — cannot convert to TypeCollection
        return element.ValueKind == JsonValueKind.Object;
#pragma warning restore FDW018
    }
}
