using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The logics a gate judges its input lines by (FDW-6134): And, Or, Not, Nand, Nor, Xor and Xnor. A plain collection: a logic has no configuration row, and a gate states which it
/// uses in its <c>logic</c> setting.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GateLogicBase), typeof(IGateLogic), typeof(GateLogics))]
public abstract partial class GateLogics : TypeCollectionBase<GateLogicBase, IGateLogic>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
