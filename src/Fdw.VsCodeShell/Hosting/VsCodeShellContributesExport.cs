using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Hosting;

/// <summary>
/// Emits the <c>contributes</c> block for the staged <c>package.json</c> from the registered command
/// collection, so commands appear in the VS Code Command Palette.
/// </summary>
/// <remarks>
/// <para>
/// VS Code sources the Command Palette from <c>contributes.commands</c> in <c>package.json</c>, which it
/// reads at install time. The bootstrap's runtime <c>registerCommand</c> calls make a command *invocable*
/// but never *visible* — so without this, no user can reach a command from the palette.
/// </para>
/// <para>
/// The collection can only be fully enumerated inside the entry-point host: commands declared in referenced
/// packages arrive via that host's generated module initializer, so neither a library source generator nor
/// MSBuild can see them. Asking the published host is therefore the only accurate source. The publish
/// target invokes it and splices the result into the staged manifest.
/// </para>
/// <para>
/// Call this as the FIRST statement in <c>Main</c>, before building the host — otherwise the export spins up
/// the full application (Kestrel, workspaces) just to print a list of names.
/// </para>
/// </remarks>
public static class VsCodeShellContributesExport
{
    private const string Flag = "--fdw-export-vscode-contributes";

    private static readonly JsonSerializerOptions CompactJson = new() { WriteIndented = false };

    /// <summary>
    /// Writes the contributes JSON and returns true when <paramref name="args"/> requests an export.
    /// The caller must return immediately when this returns true.
    /// </summary>
    public static bool TryRun(string[] args)
    {
        if (args is null)
        {
            return false;
        }

        var index = Array.FindIndex(args, a => string.Equals(a, Flag, StringComparison.Ordinal));
        if (index < 0)
        {
            return false;
        }

        if (index + 1 >= args.Length)
        {
            throw new InvalidOperationException($"{Flag} requires an output path argument.");
        }

        File.WriteAllText(args[index + 1], BuildJson());
        return true;
    }

    /// <summary>
    /// Builds the compact <c>contributes</c> JSON object for every registered command.
    /// </summary>
    /// <remarks>
    /// Single-line by design: the staging target joins the file's lines into one MSBuild property before
    /// substituting it, so a pretty-printed blob would not survive the round trip.
    /// </remarks>
    public static string BuildJson() => BuildJson(VsCodeCommandTypes.All().Values);

    /// <summary>
    /// Builds the contributes JSON from an explicit command set.
    /// </summary>
    /// <remarks>Separate overload so the serialization (and its escaping) can be tested without registering
    /// into the frozen collection.</remarks>
    public static string BuildJson(IEnumerable<IVsCodeCommandType> commandTypes)
    {
        var commands = new List<Dictionary<string, string>>();

        foreach (var command in commandTypes.OrderBy(c => c.CommandId, StringComparer.Ordinal))
        {
            var entry = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["command"] = command.CommandId,
                ["title"] = command.Title,
            };

            if (!string.IsNullOrWhiteSpace(command.PaletteCategory))
            {
                entry["category"] = command.PaletteCategory!;
            }

            commands.Add(entry);
        }

        return JsonSerializer.Serialize(
            new Dictionary<string, object>(StringComparer.Ordinal) { ["commands"] = commands },
            CompactJson);
    }
}
