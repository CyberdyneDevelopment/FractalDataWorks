using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Maps the run parameter values of a schedule between the name/value list the API carries and the dictionary the
/// schedule configuration stores in <c>sched.ScheduleParameter</c> (FDW-1110, contract v2 item 9).
/// </summary>
public static class ScheduleParameterMapper
{
    /// <summary>Builds the stored dictionary from the values a body states; names compare ignoring case, as the stored dictionary does.</summary>
    /// <param name="values">The values the body states, or <see langword="null"/> when it states none.</param>
    /// <returns>The dictionary to store.</returns>
    public static IDictionary<string, string?> ToConfiguration(IList<PipelineParameterValue>? values)
    {
        var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (values is not null)
        {
            foreach (var value in values)
            {
                parameters[value.Name] = value.Value;
            }
        }

        return parameters;
    }

    /// <summary>Reads the stored dictionary back as the list the API carries.</summary>
    /// <param name="parameters">The stored parameters.</param>
    /// <returns>The values, in name order.</returns>
    public static IList<PipelineParameterValue> ToValues(IDictionary<string, string?> parameters) =>
        [.. parameters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => new PipelineParameterValue { Name = p.Key, Value = string.Concat(p.Value) })];

    /// <summary>Whether every value is named and no name is used twice (ignoring case).</summary>
    /// <param name="values">The values a body states, or <see langword="null"/> when it states none.</param>
    /// <returns><see langword="true"/> when the names are distinct and not blank.</returns>
    public static bool NamesAreDistinct(IList<PipelineParameterValue>? values) =>
        values is null
        || (values.All(v => !string.IsNullOrWhiteSpace(v.Name))
            && values.Select(v => v.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Count);
}
