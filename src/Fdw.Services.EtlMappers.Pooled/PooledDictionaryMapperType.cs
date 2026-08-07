using Fdw.Collections.Attributes;
using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Pooled;

/// <summary>
/// Service type definition for pooled dictionary mappers.
/// </summary>
[TypeOption(typeof(EtlRowMapperTypes), "Pooled", RestrictToCurrentCompilation = true)]
public sealed class PooledDictionaryMapperType
    : EtlRowMapperTypeBase<PooledDictionaryMapper, PooledDictionaryMapperFactory, PooledDictionaryMapperConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PooledDictionaryMapperType"/> class.
    /// </summary>
    public PooledDictionaryMapperType()
        : base(
            name: "Pooled",
            sectionName: "EtlMappers:Pooled",
            displayName: "Pooled Dictionary Mapper",
            description: "Zero-allocation mapper using dictionary pooling")
    {

    }

    /// <inheritdoc />
    public override int EstimatedAllocationsPerRow => 0;

    /// <inheritdoc />


    /// <inheritdoc />
    // Why this is NOT a ServiceTypeBase phase func: this domain declares its own
    // Register contract, driven directly by its provider.
    public override IServiceCollection Register(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {

        services.AddSingleton<PooledDictionaryMapperFactory>();
        return services;
    
    
    }

}
