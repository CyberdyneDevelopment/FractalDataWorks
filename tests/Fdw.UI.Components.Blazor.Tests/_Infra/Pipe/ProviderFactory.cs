using Bunit;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Blazor.Tests.PipeInfra;

/// <summary>
/// bUnit IComponentFactory that swaps a real FDW headless provider for a
/// <see cref="ProviderStub{TContext}"/> seeded with a test context. Use one per provider type so a
/// page-component test can drive the page's rendered markup from a deterministic context.
/// </summary>
public sealed class ProviderFactory<TActual, TContext> : IComponentFactory
    where TActual : IComponent
    where TContext : new()
{
    private readonly TContext? _seed;

    public ProviderFactory(TContext? seed = default) => _seed = seed;

    public bool CanCreate(Type componentType) => componentType == typeof(TActual);

    public IComponent Create(Type componentType)
    {
        if (_seed is not null)
        {
            ProviderStubState.Set(_seed);
        }

        return new ProviderStub<TContext>();
    }
}
