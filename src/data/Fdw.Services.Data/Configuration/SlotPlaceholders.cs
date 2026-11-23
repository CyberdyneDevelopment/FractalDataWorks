using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// Fills the <c>${KEY}</c> placeholders of a text from the environment or the slot file (FDW-5847): the one mechanism behind
/// <c>configurationSchema.json</c> and behind a row that states a slot's own values.
/// </summary>
/// <remarks>
/// <para>
/// KEY is a variable of the environment or a key of the slot file (<see cref="SlotFile"/>) joined with <c>__</c>, such as
/// <c>hostnames__live__0</c>. A key stated by the environment and by the slot file is refused, and so is one stated by neither:
/// nothing stands in for a value, and a refusal says which key and where it was looked for.
/// </para>
/// <para>
/// A text with no placeholder is a literal. <see cref="Resolve"/> returns it as it is and does not read the slot file, so a row that
/// states a literal behaves exactly as it did before this existed, on a slot host or not.
/// </para>
/// </remarks>
public static partial class SlotPlaceholders
{
    [GeneratedRegex(
        @"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Placeholder();

    /// <summary>Whether a text states at least one placeholder.</summary>
    /// <param name="text">The text.</param>
    /// <returns>Whether it does.</returns>
    public static bool Has(string? text)
        => !string.IsNullOrEmpty(text) && Placeholder().IsMatch(text);

    /// <summary>Fills the placeholders of a row's text.</summary>
    /// <param name="text">The text the row states.</param>
    /// <param name="owner">What states the text, such as <c>OIDC authority 'x' AllowedRedirectOrigins</c>; every refusal says it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The text with each placeholder replaced as it is (nothing is encoded), or why one cannot be.</returns>
    public static IGenericResult<string> Resolve(string text, string owner, ILogger logger)
    {
        if (!Has(text))
            return GenericResult<string>.Success(text);

        SlotFile? slot = null;
        if (SlotFile.DeclaredPath() is { } declared)
        {
            var read = SlotFile.Read(declared, logger);
            if (read.IsFailure || read.Value is not { } file)
            {
                // The file's own failure says why it cannot be read; this one says which row and which placeholders that leaves unfilled.
                return GenericResult.Caused(read, SlotFileLog.RowSlotFileUnavailable(logger, owner, declared, Keys(text))).ToNewResult<string>();
            }

            slot = file;
        }

        var both = new List<string>();
        var missing = new List<string>();
        var resolved = Substitute(text, slot, static value => value, logger, both, missing);

        if (both.Count > 0 && slot is not null)
            return GenericResult<string>.Failure(SlotFileLog.RowPlaceholderStatedByBothSources(logger, owner, slot.FilePath, Names(both)));

        if (missing.Count > 0)
            return GenericResult<string>.Failure(slot is null
                ? SlotFileLog.RowPlaceholderNotStated(logger, owner, Names(missing))
                : SlotFileLog.RowPlaceholderNotStatedOnSlot(logger, owner, slot.FilePath, Names(missing)));

        return GenericResult<string>.Success(resolved);
    }

    /// <summary>Replaces each placeholder of a text with its value; one with no value, or two, stays as it is and is recorded.</summary>
    /// <param name="text">The text.</param>
    /// <param name="slot">The slot file, or <see langword="null"/> on a host that is not on a slot.</param>
    /// <param name="encode">How a value is written into the text (the schema loader writes it into JSON; a row's text takes it as it is).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="both">Receives each key the environment and the slot file both state.</param>
    /// <param name="missing">Receives each key neither states.</param>
    /// <returns>The text with the placeholders that had exactly one value replaced.</returns>
    internal static string Substitute(string text, SlotFile? slot, Func<string, string> encode, ILogger logger, List<string> both, List<string> missing)
        => Placeholder().Replace(text, match => ValueOf(match, slot, encode, both, missing, logger));

    /// <summary>The distinct keys a text states as placeholders, joined for a message.</summary>
    /// <param name="text">The text.</param>
    /// <returns>Each key once, separated by a comma.</returns>
    private static string Keys(string text)
        => Names([.. Placeholder().Matches(text).Select(static match => match.Groups["name"].Value)]);

    /// <summary>Joins the distinct keys of a list for a message.</summary>
    /// <param name="keys">The keys, possibly repeated.</param>
    /// <returns>Each key once, separated by a comma.</returns>
    internal static string Names(List<string> keys) => string.Join(", ", keys.Distinct(StringComparer.Ordinal));

    private static string ValueOf(Match match, SlotFile? slot, Func<string, string> encode, List<string> both, List<string> missing, ILogger logger)
    {
        var key = match.Groups["name"].Value;
        var fromEnvironment = Environment.GetEnvironmentVariable(key);
        var fromSlot = slot is not null && slot.Values.TryGetValue(key, out var stated) ? stated : null;

        if (string.IsNullOrEmpty(fromEnvironment))
        {
            if (fromSlot is null)
            {
                missing.Add(key);
                return match.Value;
            }

            SlotFileLog.PlaceholderResolved(logger, key, "the slot file");
            return encode(fromSlot);
        }

        if (fromSlot is not null)
        {
            both.Add(key);
            return match.Value;
        }

        SlotFileLog.PlaceholderResolved(logger, key, "the environment");
        return encode(fromEnvironment);
    }
}
