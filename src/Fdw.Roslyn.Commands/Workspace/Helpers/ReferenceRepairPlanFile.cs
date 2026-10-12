using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Fdw.Roslyn.Commands.Workspace.Results;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>
/// Reads and writes the hand-editable repair plan — the file a human or agent prunes to choose which
/// reference fixes get applied.
/// </summary>
/// <remarks>
/// Deleting a line is the rejection gesture. That inverts the usual approve-list: what survives in the
/// file is what runs, which is far easier to review at scale than assembling a list of ids to approve.
/// </remarks>
public static class ReferenceRepairPlanFile
{
    private const char FieldSeparator = '|';

    /// <summary>
    /// Writes the proposal file.
    /// </summary>
    /// <param name="path">The file to write.</param>
    /// <param name="proposals">The proposed repairs.</param>
    /// <param name="stamp">The timestamp to record.</param>
    /// <returns>The number of proposals written.</returns>
    public static int Write(string path, IReadOnlyList<ReferenceRepair> proposals, DateTimeOffset stamp)
    {
        if (proposals is null) throw new ArgumentNullException(nameof(proposals));

        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"# RepairMovedReferences plan — {stamp.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
        builder.AppendLine("#");
        builder.AppendLine("# DELETE any line you do NOT want applied, then re-run RepairMovedReferences");
        builder.AppendLine("# with ApplyFromPath set to this file. Whatever remains is what gets applied.");
        builder.AppendLine("# Lines starting with '#' and blank lines are ignored.");
        builder.AppendLine("#");
        builder.AppendLine("# <id> | <reference kind> | needs: <assembly> | because: <type the ledger matched>");
        builder.AppendLine();

        foreach (var proposal in proposals)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{proposal.Id} {FieldSeparator} {DescribeKind(proposal)} {FieldSeparator} needs: {proposal.RequiredAssembly} {FieldSeparator} because: {proposal.LedgerMatch}");
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory!);
        File.WriteAllText(path, builder.ToString());

        return proposals.Count;
    }

    /// <summary>
    /// Reads the ids that survived pruning.
    /// </summary>
    /// <param name="path">The plan file.</param>
    /// <returns>The approved ids, in file order.</returns>
    public static IReadOnlyList<string> ReadApprovedIds(string path)
    {
        var approved = new List<string>();

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#') continue;

            var separator = trimmed.IndexOf(FieldSeparator);
            var id = (separator < 0 ? trimmed : trimmed.Substring(0, separator)).Trim();

            if (id.Length > 0) approved.Add(id);
        }

        return approved;
    }

    private static string DescribeKind(ReferenceRepair proposal) =>
        string.IsNullOrEmpty(proposal.ReferenceKind) ? "reference" : proposal.ReferenceKind;
}
