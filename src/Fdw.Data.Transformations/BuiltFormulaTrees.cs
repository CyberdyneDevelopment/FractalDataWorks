using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Data.Transformations;

/// <summary>
/// Reads the stored pieces of one formula into the engine's tree and writes a tree out as pieces, for every kind of node row.
/// </summary>
/// <remarks>
/// Reading refuses, with a coded result, rows that do not form exactly one tree; it does not repair them. Pieces are numbered
/// in pre-order, so the root is always piece 0.
/// </remarks>
internal static class BuiltFormulaTrees
{
    /// <summary>Reads the pieces of one formula.</summary>
    /// <param name="owner">The transformation the formula belongs to, for a refusal.</param>
    /// <param name="label">Which formula it is, for a refusal.</param>
    /// <param name="rows">Exactly the pieces of this one formula.</param>
    /// <param name="maxDepth">The depth limit that bounds how far a stored tree is followed.</param>
    /// <returns>The tree, or the coded refusal.</returns>
    internal static IGenericResult<CalculationNode> Read(string owner, string label, IReadOnlyList<IBuiltFormulaNodeRow> rows, int maxDepth)
    {
        var refusal = FindShapeProblem(rows);
        if (refusal is not null)
        {
            return Refuse(owner, label, refusal);
        }

        var reading = new BuiltTransformationTreeReading(
            owner,
            label,
            maxDepth,
            rows.Where(row => row.ParentNodeNumber is not null).ToLookup(row => row.ParentNodeNumber!.Value));
        var tree = Build(rows.Single(row => row.ParentNodeNumber is null), reading, 1);
        if (tree.IsSuccess && reading.PiecesUsed != rows.Count)
        {
            return Refuse(owner, label, string.Create(CultureInfo.InvariantCulture, $"{rows.Count - reading.PiecesUsed} of its pieces are not reachable from the root"));
        }

        return tree;
    }

    /// <summary>Writes a tree as pieces, root first.</summary>
    /// <typeparam name="TRow">The kind of node row to write.</typeparam>
    /// <param name="node">The tree.</param>
    /// <param name="create">Makes one empty row, with whatever identifies the formula already set.</param>
    /// <returns>The pieces, numbered in pre-order.</returns>
    internal static List<TRow> Flatten<TRow>(CalculationNode node, Func<TRow> create)
        where TRow : IBuiltFormulaNodeRow
    {
        var rows = new List<TRow>();
        Add(node, null, 0, create, rows);
        return rows;
    }

    private static void Add<TRow>(CalculationNode node, int? parent, int position, Func<TRow> create, List<TRow> rows)
        where TRow : IBuiltFormulaNodeRow
    {
        var number = rows.Count;
        var row = create();
        row.NodeNumber = number;
        row.ParentNodeNumber = parent;
        row.Position = position;
        row.Operator = node.Op;
        row.NodeName = node.Name;
        row.NumberValue = node.Value;
        row.FlagValue = node.Flag;
        row.Places = node.Places;
        row.Mode = node.Mode;
        rows.Add(row);
        for (var index = 0; index < node.Operands.Count; index++)
        {
            Add(node.Operands[index], number, index, create, rows);
        }
    }

    // Counts what must hold before a tree can be read: some rows, no repeated number, exactly one root.
    private static string? FindShapeProblem(IReadOnlyList<IBuiltFormulaNodeRow> rows)
    {
        if (rows.Count == 0)
        {
            return "it has no formula rows";
        }

        if (rows.Select(row => row.NodeNumber).Distinct().Count() != rows.Count)
        {
            return "a piece number is used twice";
        }

        var roots = rows.Count(row => row.ParentNodeNumber is null);
        return roots == 1 ? null : string.Create(CultureInfo.InvariantCulture, $"it has {roots} root pieces, expected one");
    }

    private static IGenericResult<CalculationNode> Build(IBuiltFormulaNodeRow row, BuiltTransformationTreeReading reading, int depth)
    {
        if (depth > reading.MaxDepth)
        {
            return Refuse(reading.Owner, reading.Output, string.Create(CultureInfo.InvariantCulture, $"its formula is deeper than the limit of {reading.MaxDepth}"));
        }

        reading.PiecesUsed++;
        var operands = new List<CalculationNode>();
        foreach (var child in reading.ChildrenOf(row.NodeNumber))
        {
            var operand = Build(child, reading, depth + 1);
            if (operand.IsFailure)
            {
                return operand;
            }

            operands.Add(operand.Value!);
        }

        return GenericResult<CalculationNode>.Success(
            new CalculationNode(row.Operator, row.NodeName, row.NumberValue, row.FlagValue, row.Places, row.Mode, operands));
    }

    private static IGenericResult<CalculationNode> Refuse(string owner, string label, string reason) =>
        GenericResult<CalculationNode>.Failure(
            CalculationTreeResultCodes.ByName("StoredFormulaInvalid"),
            ResultDetails.Create("node", owner + "/" + label, "reason", reason));
}
