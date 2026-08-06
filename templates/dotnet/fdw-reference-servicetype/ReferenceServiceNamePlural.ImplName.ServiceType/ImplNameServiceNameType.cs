using System;
using Fdw.Collections;
using Fdw.Services.ServiceNamePlural;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReferenceServiceNamePlural.ImplName;

/// <summary>
/// Service-type registration for the ImplName ServiceName.
/// </summary>
// Why: this type — and this package — is the REGISTRATION, separate from the aggregation it
// registers. Referencing this enlists the ServiceNamePlural domain; referencing only the aggregation
// package composes the service and enlists nothing. That choice is the reason for the split.
//
// Why the namespace matches the aggregation's: only the owning ASSEMBLY differs. Keeping the
// namespace means the fully-qualified name is unchanged, so this option's FNV-1a Id — derived from
// the FQN — does not move, and no persisted configuration has to be rewritten.
//
// TODO: close ServiceNameTypeBase on YOUR domain's real generic arguments. Most domains use
// <TService, TFactory, TConfiguration>, but check — the arity and the constructor parameters differ
// per domain, and the compiler is the only reliable oracle.
[ServiceTypeOption(typeof(ServiceNameTypes), "ImplName")]
public sealed class ImplNameServiceNameType
    : ServiceNameTypeBase<ImplNameServiceName, IImplNameServiceNameFactory, ImplNameServiceNameConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplNameServiceNameType"/> class.
    /// </summary>
    public ImplNameServiceNameType()
        : base(
            name: "ImplName",
            displayName: "ImplName ServiceName",
            description: "Reference ImplName implementation of the ServiceName domain.")
    {
    }

    /// <summary>
    /// Phase 1 — register the factory and everything it needs with the container.
    /// </summary>
    /// <param name="services">The service collection being built.</param>
    /// <param name="loggerFactory">Optional logger factory available during registration.</param>
    /// <returns>The same collection, for chaining.</returns>
    public override IServiceCollection RegisterRequiredServices(
        IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Why: registered against the INTERFACE so the option never binds to the concrete
        // aggregation. DI supplies the constructor dependencies.
        services.AddSingleton<IImplNameServiceNameFactory, ImplNameServiceNameFactory>();

        return services;
    }
}
