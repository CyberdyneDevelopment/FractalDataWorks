using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// TypeCollection for operation parameter kinds.
/// Source generator discovers all types decorated with
/// <c>[TypeOption(typeof(OperationParameterKinds), ...)]</c> and generates
/// <c>All()</c>, <c>ById()</c>, <c>ByName()</c>, and <c>NotFound()</c> members.
/// </summary>
[TypeCollection(typeof(OperationParameterKindBase), typeof(IOperationParameterKind), typeof(OperationParameterKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class OperationParameterKinds : TypeCollectionBase<OperationParameterKindBase, IOperationParameterKind>
{
}
