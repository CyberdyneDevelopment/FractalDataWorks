using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>The T-SQL value-source renderers, found by value-source kind.</summary>
[TypeCollection(typeof(MsSqlValueSourceRendererBase), typeof(IMsSqlValueSourceRenderer), typeof(MsSqlValueSourceRenderers))]
[ExcludeFromCodeCoverage]
public abstract partial class MsSqlValueSourceRenderers :
    TypeCollectionBase<MsSqlValueSourceRendererBase, IMsSqlValueSourceRenderer>
{
}
