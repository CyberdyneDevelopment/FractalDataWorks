using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Sets;

namespace Fdw.Services.Calculations.Sets;

/// <summary>One output of a compiled set: its definition, its compiled formulas, and the derived values it actually reads.</summary>
internal sealed class CompiledOutput
{
    private readonly IReadOnlyList<ICompiledCalculation> _sources;
    private readonly ICompiledCalculation? _columns;
    private readonly IReadOnlyList<DerivedValueDefinition> _derived;

    internal CompiledOutput(
        TableOutputDefinition definition,
        IReadOnlyList<ICompiledCalculation> sources,
        ICompiledCalculation? columns,
        IReadOnlyList<DerivedValueDefinition> derived)
    {
        Definition = definition;
        _sources = sources;
        _columns = columns;
        _derived = derived;
    }

    internal TableOutputDefinition Definition { get; }

    internal IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Evaluate(SetRunScope scope, SetRowFailureHandler onRowFailure)
    {
        var drivers = Definition.Rows.Drivers(Definition.Name, scope, _sources);
        if (drivers.IsFailure)
        {
            return drivers.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        }

        var rows = new List<IReadOnlyDictionary<string, object?>>(drivers.Value!.Count);
        var state = new CalculationRunState();
        foreach (var driver in drivers.Value)
        {
            var row = Build(driver, scope, state);
            if (row.IsFailure)
            {
                // Why the caller decides: a row that cannot be built is dropped or fails the set by the caller's rule, never by a default here.
                var failure = new SetRowFailure(Definition.Name, driver.Values, string.Join("; ", row.Messages.Select(message => message.Message)));
                if (onRowFailure(failure))
                {
                    continue;
                }

                return row.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
            }

            rows.Add(row.Value!);
        }

        return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows);
    }

    private IGenericResult<IReadOnlyDictionary<string, object?>> Build(DriverRow driver, SetRunScope scope, CalculationRunState state)
    {
        var record = new Dictionary<string, object?>(driver.Values, System.StringComparer.Ordinal);
        foreach (var value in _derived)
        {
            var computed = value.Compute(Definition.Name, driver, scope);
            if (computed.IsFailure)
            {
                return computed.ToNewResult<IReadOnlyDictionary<string, object?>>();
            }

            record[value.Name] = computed.Value;
        }

        var results = Formulas(record, state);
        if (results.IsFailure)
        {
            return results.ToNewResult<IReadOnlyDictionary<string, object?>>();
        }

        var context = new OutputRowContext(Definition.Name, record, results.Value!);
        var row = new Dictionary<string, object?>(Definition.Columns.Count, System.StringComparer.Ordinal);
        foreach (var column in Definition.Columns)
        {
            row[column.Name] = column.Value(context);
        }

        return context.Failure is { } failure
            ? failure.ToNewResult<IReadOnlyDictionary<string, object?>>()
            : GenericResult<IReadOnlyDictionary<string, object?>>.Success(row);
    }

    private IGenericResult<IReadOnlyDictionary<string, decimal>> Formulas(Dictionary<string, object?> record, CalculationRunState state)
    {
        var computed = new Dictionary<string, decimal>(System.StringComparer.Ordinal);
        if (_columns is null)
        {
            return GenericResult<IReadOnlyDictionary<string, decimal>>.Success(computed);
        }

        var results = new decimal[_columns.EmittedNames.Count];
        if (!_columns.TryEvaluate(record, results, state))
        {
            return _columns.Describe(state).ToNewResult<IReadOnlyDictionary<string, decimal>>();
        }

        for (var index = 0; index < results.Length; index++)
        {
            computed[_columns.EmittedNames[index]] = results[index];
        }

        return GenericResult<IReadOnlyDictionary<string, decimal>>.Success(computed);
    }
}
