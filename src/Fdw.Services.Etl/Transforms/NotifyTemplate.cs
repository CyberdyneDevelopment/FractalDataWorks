using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Reads and fills the <c>{{Variable}}</c> references of a Notify message (FDW-1120).
/// </summary>
/// <remarks>
/// Why unknown variables are refused: a typo left in the text would reach the channel as the literal braces, so the message
/// names the reserved variables a run has (the options of <see cref="NotifyVariables"/>) and anything else is refused when the
/// stage is saved and again when the run starts, never sent as written.
/// </remarks>
internal static class NotifyTemplate
{
    private static readonly Regex Variable = new(
        @"\{\{\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>Gets the names of the variables a message may use, as a list to show in a refusal.</summary>
    public static string Reserved => string.Join(", ", NotifyVariables.All().Select(variable => variable.Name));

    /// <summary>Finds a variable by name, ignoring case.</summary>
    /// <param name="name">The name inside the braces.</param>
    /// <returns>The variable, or null when no run has one of that name.</returns>
    public static INotifyVariable? Find(string name) =>
        NotifyVariables.All().FirstOrDefault(variable => string.Equals(variable.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Lists the variables a message references that no run has, each once, in order of first appearance.</summary>
    /// <param name="template">The message template.</param>
    /// <returns>The unknown names; empty when every reference is reserved.</returns>
    public static IReadOnlyList<string> Unknown(string template) =>
        Variable.Matches(template)
            .Select(match => match.Groups["name"].Value)
            .Where(name => Find(name) is null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Fills every reference in a message with its value for the run.</summary>
    /// <param name="stageName">The Notify stage the message belongs to, for the refusal.</param>
    /// <param name="template">The message template.</param>
    /// <param name="facts">What the run can say about itself.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The filled message, or a failure naming the unknown variables.</returns>
    public static IGenericResult<string> Expand(string stageName, string template, NotifyRunFacts facts, ILogger logger)
    {
        var unknown = Unknown(template);
        return unknown.Count > 0
            ? GenericResult<string>.Failure(NotifyLog.NotifyTemplateUnknownVariable(
                logger, stageName, string.Join(", ", unknown), Reserved))
            : GenericResult<string>.Success(Variable.Replace(template, match => Find(match.Groups["name"].Value)!.ValueOf(facts)));
    }
}
