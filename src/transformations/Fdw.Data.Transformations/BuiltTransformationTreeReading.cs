using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;

namespace Fdw.Data.Transformations;

/// <summary>What one reading of a stored formula carries along: who it belongs to, the depth limit, and how far it has got.</summary>
internal sealed class BuiltTransformationTreeReading
{
    public BuiltTransformationTreeReading(
        string owner,
        string output,
        int maxDepth,
        ILookup<int, IBuiltFormulaNodeRow> childrenByParent)
    {
        Owner = owner;
        Output = output;
        MaxDepth = maxDepth;
        ChildrenByParent = childrenByParent;
    }

    public string Owner { get; }

    public string Output { get; }

    public int MaxDepth { get; }

    public ILookup<int, IBuiltFormulaNodeRow> ChildrenByParent { get; }

    public int PiecesUsed { get; set; }

    public IEnumerable<IBuiltFormulaNodeRow> ChildrenOf(int parent) =>
        ChildrenByParent[parent].OrderBy(child => child.Position);
}
