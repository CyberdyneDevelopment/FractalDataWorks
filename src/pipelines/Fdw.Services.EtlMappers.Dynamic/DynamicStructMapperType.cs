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
            displayName: "Dynamic Struct Mapper",
            description: "Mapper using compiled expression trees for field access")
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
        => _declaredDefaults ??= ServiceTypeDefaults.With(base.DeclaredDefaults, (nameof(DynamicStructMapperConfiguration.EnablePooling), true), (nameof(DynamicStructMapperConfiguration.MaxPoolSize), 1000), (nameof(DynamicStructMapperConfiguration.UseCompiledExpressions), true), (nameof(DynamicStructMapperConfiguration.CacheCompiledDelegates), true));

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
