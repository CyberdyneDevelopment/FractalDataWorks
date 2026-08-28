using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Default implementation of row mapping context.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class DefaultRowMappingContext : RowMappingContextBase
{
    public DefaultRowMappingContext(int[] ordinals, string[] names, IDataTypeConverter?[] converters)
        : base(ordinals, names, converters)
    {
    }
}