using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Dynamic;

/// <summary>
/// Configuration for the dynamic struct mapper.
/// </summary>
public sealed class DynamicStructMapperConfiguration : EtlRowMapperConfiguration
{
    /// <inheritdoc />
    public override string MapperType => "Dynamic";

    /// <summary>
    /// Gets or sets whether to use compiled expressions for field access.
    /// When true, uses compiled delegates for faster access.
    /// When false, uses reflection (slower but simpler).
    /// </summary>
    public bool UseCompiledExpressions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to cache compiled delegates.
    /// </summary>
    public bool CacheCompiledDelegates { get; set; } = true;
}
