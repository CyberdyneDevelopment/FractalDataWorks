using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The token <c>$(Name)</c> in the text of a SQL script (FDW-1970): replaced by the value of the parameter or variable <c>Name</c> before the script
/// runs, for the names a SQL parameter cannot carry (a schema, a table, a column). Values stay <c>@parameters</c>, bound by the connection.
/// </summary>
/// <remarks>
/// Why a value must be a name: it is put into SQL text, so anything but letters, digits, underscore and dot would let a caller write SQL. A value
/// that is not a name fails the script and is not echoed; there is no escaping and no fallback.
/// </remarks>
public static partial class ScriptTokens
{
    [GeneratedRegex(@"\$\(\s*(?<name>[^()\s]+)\s*\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Token();

    [GeneratedRegex(@"^[A-Za-z0-9_.]+$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ASqlName();

    /// <summary>Whether the text holds a token.</summary>
    /// <param name="text">The script text.</param>
    /// <returns><see langword="true"/> when it holds at least one <c>$(Name)</c>.</returns>
    public static bool Holds(string? text) => !string.IsNullOrEmpty(text) && Token().IsMatch(text);

    /// <summary>Lists the names the tokens of a text refer to.</summary>
    /// <param name="text">The script text.</param>
    /// <returns>The names, in the order they appear; a name used twice is listed twice.</returns>
    public static IReadOnlyList<string> Names(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return [.. Token().Matches(text).Select(match => match.Groups["name"].Value)];
    }

    /// <summary>Replaces every token with the value of the parameter or variable it names.</summary>
    /// <param name="owner">The step or component the script belongs to, for the failure.</param>
    /// <param name="text">The script text.</param>
    /// <param name="values">The parameters and variables as they are now, by name.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The text to run, or the failure that names the owner and the name that has no value or whose value is not a name.</returns>
    public static IGenericResult<string> Render(string owner, string text, IReadOnlyDictionary<string, object?> values, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(logger);
        IGenericMessage? refused = null;
        var rendered = Token().Replace(text, match =>
        {
            if (refused is not null)
            {
                return match.Value;
            }

            var name = match.Groups["name"].Value;
            var found = values.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));
            if (found.Key is null || found.Value is null)
            {
                refused = FlowLog.ScriptTokenNotGiven(logger, owner, name);
                return match.Value;
            }

            var value = Convert.ToString(found.Value, CultureInfo.InvariantCulture);
            if (value is null || !ASqlName().IsMatch(value))
            {
                refused = FlowLog.ScriptTokenNotAName(logger, owner, name);
                return match.Value;
            }

            return value;
        });
        return refused is null ? GenericResult<string>.Success(rendered) : GenericResult<string>.Failure(refused);
    }
}
