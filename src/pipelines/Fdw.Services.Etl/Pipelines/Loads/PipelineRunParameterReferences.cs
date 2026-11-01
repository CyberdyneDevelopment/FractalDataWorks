using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Finds and resolves the <c>{{name}}</c> run parameter references inside an expression (FDW-1109).
/// </summary>
/// <remarks>
/// <para>
/// A reference is replaced by the invariant text of the parameter's typed value. A reference with no value for the run is
/// refused, never replaced by an empty text or a default: <c>{{LastSuccessfulRun}}</c> gets its own refusal because it is
/// the case a first run hits.
/// </para>
/// <para>
/// Why a string value is checked: the expression languages that carry references split an expression on their own
/// operators and quotes, so a text value holding one could change what the expression means. A text value is therefore
/// placed only when it holds letters, digits and the few separators <see cref="IsPlaceable"/> lists, and holds no logical
/// word; anything else is refused with the reason.
/// </para>
/// </remarks>
public static class PipelineRunParameterReferences
{
    private static readonly Regex Reference = new(
        @"\{\{\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly string[] LogicalWords = [" and ", " or ", " not "];

    /// <summary>Lists the parameter names an expression references, each once, in order of first appearance.</summary>
    /// <param name="expression">The expression text, or <see langword="null"/>.</param>
    /// <returns>The names.</returns>
    public static IReadOnlyList<string> Names(string? expression)
    {
        if (string.IsNullOrEmpty(expression))
            return [];
        return Reference.Matches(expression)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Replaces every reference in an expression by the text of its value.</summary>
    /// <param name="owner">What the expression belongs to, for the refusal message.</param>
    /// <param name="expression">The expression text.</param>
    /// <param name="values">The run's typed parameter values, including the watermark when there is one.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>The expression with its references replaced, or a failure naming the reference.</returns>
    public static IGenericResult<string> Resolve(
        string owner,
        string expression,
        IReadOnlyDictionary<string, object?> values,
        ILogger logger)
    {
        var builder = new StringBuilder(expression.Length);
        var last = 0;
        foreach (Match match in Reference.Matches(expression))
        {
            var text = TextOf(owner, match.Groups["name"].Value, values, logger);
            if (text.IsFailure)
                return text;
            builder.Append(expression, last, match.Index - last).Append(text.Value);
            last = match.Index + match.Length;
        }

        return GenericResult<string>.Success(builder.Append(expression, last, expression.Length - last).ToString());
    }

    private static IGenericResult<string> TextOf(string owner, string name, IReadOnlyDictionary<string, object?> values, ILogger logger)
    {
        if (!values.TryGetValue(name, out var value) || value is null)
        {
            return GenericResult<string>.Failure(string.Equals(name, PipelineRunParameterValues.LastSuccessfulRun, StringComparison.OrdinalIgnoreCase)
                ? PipelineRunLog.LastSuccessfulRunMissing(logger, owner)
                : PipelineRunLog.RunParameterReferenceUnresolved(logger, owner, name));
        }

        var type = DataTypes.All().FirstOrDefault(candidate => candidate.RendersInExpressions && candidate.ClrType == value.GetType());
        if (type is null)
            return GenericResult<string>.Failure(PipelineRunLog.RunParameterValueUnusable(
                logger, owner, name, "its type is not one a run parameter can have"));

        var text = type.Format(value);
        return value is string && !IsPlaceable(text)
            ? GenericResult<string>.Failure(PipelineRunLog.RunParameterValueUnusable(
                logger, owner, name, "a text value holds only letters, digits, spaces and _ - . : / @ + , and no 'and', 'or' or 'not' word"))
            : GenericResult<string>.Success(text);
    }

    private static bool IsPlaceable(string text)
    {
        foreach (var character in text)
        {
            if (!char.IsLetterOrDigit(character) && " _-.:/@+,".IndexOf(character) < 0)
                return false;
        }

        var padded = " " + text + " ";
        return !LogicalWords.Any(word => padded.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
