using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What a run's record says about the values it was given (FDW-1756): for each declared parameter the run supplied, that it was given and,
/// only when its author opted the parameter in, the value as text.
/// </summary>
/// <remarks>
/// <para>
/// Why the value of a parameter that did not opt in is never read into the entries: nothing that reads the record (a list, the events, the
/// log) can leak what was never written, so the choice is made here, before anything is recorded.
/// </para>
/// <para>
/// A value is written as text the way it arrived: a string as itself, a true or false in lower case, a date as ISO 8601, a number in the
/// invariant culture, and a list as its JSON text. Parameters the pipeline does not declare, and the reserved <c>LastSuccessfulRun</c>, have no
/// entry.
/// </para>
/// </remarks>
public static class RunParameterEntries
{
    /// <summary>The run event that carries the entries.</summary>
    public const string EventType = "parameters.given";

    /// <summary>Builds the data of the run event, one entry per declared parameter the run was given.</summary>
    /// <param name="declared">The pipeline's declared parameters, or <see langword="null"/> when it declares none.</param>
    /// <param name="values">The run's typed values by parameter name.</param>
    /// <param name="defaulted">The names of the parameters whose value is the default the pipeline declares, not one the run gave (FDW-1975): they are recorded as not given; none when no parameter took a default.</param>
    /// <returns>The event data, or <see langword="null"/> when the run was given no declared parameter.</returns>
    public static IReadOnlyDictionary<string, object?>? EventData(
        IEnumerable<IPipelineRunParameter>? declared, IReadOnlyDictionary<string, object?> values, IReadOnlySet<string>? defaulted = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (declared is null)
        {
            return null;
        }

        var entries = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var parameter in declared)
        {
            if (!values.TryGetValue(parameter.Name, out var value) || value is null)
            {
                continue;
            }

            var entry = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = parameter.Name,
                ["given"] = defaulted is null || !defaulted.Contains(parameter.Name),
            };
            if (parameter.ShowsValue)
            {
                entry["value"] = Text(value);
            }

            entries.Add(entry);
        }

        return entries.Count == 0 ? null : new Dictionary<string, object?>(StringComparer.Ordinal) { ["parameters"] = entries };
    }

    private static string Text(object value) => value switch
    {
        string text => text,
        bool flag => flag ? "true" : "false",
        DateTimeOffset moment => moment.ToString("O", CultureInfo.InvariantCulture),
        DateTime moment => moment.ToString("O", CultureInfo.InvariantCulture),
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => JsonSerializer.Serialize(value),
    };
}
