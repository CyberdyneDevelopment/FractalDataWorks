using Fdw.Collections.Attributes;
using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Dynamic;

/// <summary>
/// Service type definition for dynamic struct mappers.
/// </summary>
[TypeOption(typeof(EtlRowMapperTypes), "Dynamic", RestrictToCurrentCompilation = true)]
public sealed class DynamicStructMapperType
    : EtlRowMapperTypeBase<DynamicStructMapper, DynamicStructMapperFactory, DynamicStructMapperConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicStructMapperType"/> class.
    /// </summary>
    public DynamicStructMapperType()
        : base(
            name: "Dynamic",
            sectionName: "EtlMappers:Dynamic",
            displayName: "Dynamic Struct Mapper",
            description: "Mapper using compiled expression trees for field access")
    {

    }

    /// <inheritdoc />
    public override int EstimatedAllocationsPerRow => 1;

    /// <inheritdoc />


    /// <inheritdoc />
    public override IServiceCollection Register(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {

        services.AddSingleton<DynamicStructMapperFactory>();
        return services;
    
    
    }

}
