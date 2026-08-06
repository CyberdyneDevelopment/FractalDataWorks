using Bunit;
using Bunit.ComponentFactories;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Blazor.Tests.ConnInfra;

/// <summary>
/// bUnit IComponentFactory that swaps a real FDW provider for a
/// <see cref="ProviderStub{TContext}"/> seeded with a test context. Use one per provider type.
/// </summary>
public sealed class ProviderFactory<TActual, TContext> : IComponentFactory
    where TActual : IComponent
    where TContext : new()
{
    private readonly TContext? _seed;

    public ProviderFactory(TContext? seed = default) => _seed = seed;

    public bool CanCreate(Type componentType) => componentType == typeof(TActual);

    // Why: pass the seed straight into the stub instance instead of a shared static, so stub-based
    // tests from different classes running in parallel never read each other's pending context.
    public IComponent Create(Type componentType) => new ProviderStub<TContext>(_seed);
}
