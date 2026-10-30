using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Splits the rows of a Files destination into the files they go to (FDW-3193): one file for each value of one field, named by that value and holding
/// only the rows with it. Every row and every value is judged before anything is returned, so a value in the last row that cannot name a file leaves no
/// file; nothing is made up for a blank value and no value is changed to make it a name.
/// </summary>
public static class LoadFileSplit
{
    private const int LongestValue = 150;
    private static readonly char[] RefusedCharacters = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>Splits the rows.</summary>
    /// <param name="owner">What the load is, for refusal messages.</param>
    /// <param name="extension">The extension of the declared file, with its dot, added to each value to make the file name.</param>
    /// <param name="splitField">The field whose value names the file.</param>
    /// <param name="rows">The rows of the run.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>The files with their rows, in the order their values first appear, or the failure naming the field, row or value.</returns>
    public static IGenericResult<IReadOnlyList<LoadFileRows>> Group(
        string owner, string extension, string splitField, IReadOnlyList<IDictionary<string, object?>> rows, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
            return GenericResult<IReadOnlyList<LoadFileRows>>.Success([]);

        var key = rows[0].Keys.FirstOrDefault(name => string.Equals(name, splitField, StringComparison.OrdinalIgnoreCase));
        if (key is null)
            return GenericResult<IReadOnlyList<LoadFileRows>>.Failure(
                PipelineRunLog.SplitFieldNotInRows(logger, owner, splitField, string.Join(", ", rows[0].Keys)));

        var groups = new Dictionary<string, List<IDictionary<string, object?>>>(StringComparer.Ordinal);
        for (var index = 0; index < rows.Count; index++)
        {
            var value = rows[index].TryGetValue(key, out var held) ? Convert.ToString(held, CultureInfo.InvariantCulture) : null;
            if (string.IsNullOrWhiteSpace(value))
                return GenericResult<IReadOnlyList<LoadFileRows>>.Failure(PipelineRunLog.SplitValueBlank(logger, owner, key, index + 1));

            if (!groups.TryGetValue(value, out var group))
                groups[value] = group = [];

            group.Add(rows[index]);
        }

        var refused = Refused(owner, groups.Keys, logger);
        return refused is not null
            ? GenericResult<IReadOnlyList<LoadFileRows>>.Failure(refused)
            : GenericResult<IReadOnlyList<LoadFileRows>>.Success([.. groups.Select(group => new LoadFileRows(string.Concat(group.Key, extension), group.Value))]);
    }

    // The first value that cannot name a file, or the first two that name the same one on a store that ignores case.
    private static IGenericMessage? Refused(string owner, IEnumerable<string> values, ILogger logger)
    {
        foreach (var value in values)
        {
            if (Reason(value) is { } reason)
                return PipelineRunLog.SplitValueInvalid(logger, owner, value, reason);
        }

        var same = values.GroupBy(value => value, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        return same is null ? null : PipelineRunLog.SplitValuesCollide(logger, owner, same.First(), same.Skip(1).First());
    }

    // Why a value cannot name a file; nothing reaches outside the folder the file is written in.
    private static string? Reason(string value)
    {
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            return "it starts or ends with a space";

        if (value.StartsWith('.') || value.EndsWith('.'))
            return "it starts or ends with a dot";

        if (value.Contains("..", StringComparison.Ordinal))
            return "it contains '..'";

        if (value.Length > LongestValue)
            return $"it is longer than {LongestValue} characters";

        var refused = value.FirstOrDefault(character => char.IsControl(character) || Array.IndexOf(RefusedCharacters, character) >= 0);
        return refused == default ? null : char.IsControl(refused) ? "it contains a control character" : $"it contains the character '{refused}'";
    }
}
