using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Default implementation of row mapping context.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class DefaultRowMappingContext : RowMappingContextBase
{
    public DefaultRowMappingContext(int[] ordinals, string[] names, IDataTypeConverter?[] converters)
        : base(ordinals, names, converters)
    {
    }
}