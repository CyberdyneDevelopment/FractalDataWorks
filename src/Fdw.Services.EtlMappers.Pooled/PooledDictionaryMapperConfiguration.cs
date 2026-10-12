using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Pooled;

/// <summary>
/// Configuration for the pooled dictionary mapper.
/// </summary>
public sealed class PooledDictionaryMapperConfiguration : EtlRowMapperConfiguration
{
    /// <inheritdoc />
    public override string MapperType => "Pooled";

    /// <summary>
    /// Gets or sets the maximum dictionary size to pool.
    /// Dictionaries larger than this are not returned to the pool.
    /// </summary>
    public int MaxDictionarySize { get; set; } = 100;
}
