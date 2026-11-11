using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Results;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Reads the parameters a stored threshold states (FDW-3935) and checks them against what its type declares: a parameter the type does not declare, a required one that is
/// missing, a single-valued one stated twice, a list one stated with no value, and a value that is not of the declared type are each refused. Nothing is assumed or repaired.
/// </summary>
public static class ThresholdParameterReader
{
    /// <summary>Reads the parameters.</summary>
    /// <param name="type">The threshold type, which declares the parameters it takes.</param>
    /// <param name="threshold">The threshold name, for a failure message.</param>
    /// <param name="stored">The stored parameter rows.</param>
    /// <returns>The parameters by name: one value, or the values in order for a list parameter; a failure naming what is wrong.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> From(IThreshold type, string threshold, IEnumerable<ThresholdParameterConfiguration> stored)
    {
        var rows = stored.OrderBy(row => row.Name, StringComparer.Ordinal).ThenBy(row => row.Ordinal).ToList();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var group in rows.GroupBy(row => row.Name, StringComparer.Ordinal))
        {
            var declared = type.Parameters.FirstOrDefault(declaration => string.Equals(declaration.Name, group.Key, StringComparison.Ordinal));
            if (declared is null)
            {
                return Invalid(threshold, group.Key, $"the type '{type.Name}' declares no such parameter");
            }

            var read = Read(declared, threshold, group.ToList());
            if (read.IsFailure)
            {
                return read.ToNewResult<IReadOnlyDictionary<string, object?>>();
            }

            parameters[declared.Name] = read.Value;
        }

        var missing = type.Parameters.FirstOrDefault(declaration => declaration.IsRequired && !parameters.ContainsKey(declaration.Name));
        return missing is null
            ? GenericResult<IReadOnlyDictionary<string, object?>>.Success(parameters)
            : Invalid(threshold, missing.Name, "it is required by the type and is not stated");
    }

    private static IGenericResult<object?> Read(ThresholdParameterDeclaration declared, string threshold, List<ThresholdParameterConfiguration> rows)
    {
        if (!declared.IsList && rows.Count != 1)
        {
            return InvalidValue(threshold, declared.Name, "it takes one value and is stated more than once");
        }

        var values = new List<object?>();
        foreach (var row in rows)
        {
            var value = row.Value is { } text ? ThresholdValueText.Read(row.DataType, text) : null;
            if (value is null || !declared.ValueType.IsInstanceOfType(value))
            {
                return InvalidValue(threshold, declared.Name, $"its value is missing or is not a {declared.ValueType.Name} read as {row.DataType}");
            }

            values.Add(value);
        }

        return GenericResult<object?>.Success(declared.IsList ? values : values[0]);
    }

    private static IGenericResult<IReadOnlyDictionary<string, object?>> Invalid(string threshold, string parameter, string reason) =>
        ThresholdRuleFailures.ParameterInvalid(threshold, parameter, reason).ToNewResult<IReadOnlyDictionary<string, object?>>();

    private static IGenericResult<object?> InvalidValue(string threshold, string parameter, string reason) =>
        ThresholdRuleFailures.ParameterInvalid(threshold, parameter, reason).ToNewResult<object?>();
}
