using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.ServiceTypes;
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
            displayName: "Pooled Dictionary Mapper",
            description: "Zero-allocation mapper using dictionary pooling")
    {

    }

    /// <inheritdoc />
    private IReadOnlyDictionary<string, object>? _declaredDefaults;

    /// <inheritdoc />
    /// <remarks>
    /// The values a configuration of this mapper takes for a field it leaves out (FDW-3956, Mike: "a default value set in the service type
    /// option or collection so that in the registration we can change it"). A host changes one with <c>ServiceTypeDefaults.Override</c>.
    /// </remarks>
    public override IReadOnlyDictionary<string, object> DeclaredDefaults
        => _declaredDefaults ??= ServiceTypeDefaults.With(base.DeclaredDefaults, (nameof(PooledDictionaryMapperConfiguration.EnablePooling), true), (nameof(PooledDictionaryMapperConfiguration.MaxPoolSize), 1000), (nameof(PooledDictionaryMapperConfiguration.MaxDictionarySize), 100));

    /// <inheritdoc />
    public override int EstimatedAllocationsPerRow => 0;

    /// <inheritdoc />


    /// <inheritdoc />
    public override IServiceCollection Register(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {

        services.AddSingleton<PooledDictionaryMapperFactory>();
        return services;
    
    
    }

}
