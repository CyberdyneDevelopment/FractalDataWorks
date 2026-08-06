using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// TypeCollection for scalar value types supported as calculation inputs.
/// Source generator discovers all types decorated with [TypeOption(typeof(ScalarValueTypes), ...)] and generates All(), ById(), ByName(), and NotFound() members.
/// </summary>
[TypeCollection(typeof(ScalarValueTypeBase), typeof(IScalarValueType), typeof(ScalarValueTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ScalarValueTypes : TypeCollectionBase<ScalarValueTypeBase, IScalarValueType>
{
}
