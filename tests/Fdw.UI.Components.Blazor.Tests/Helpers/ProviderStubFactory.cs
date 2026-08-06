using Bunit;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Blazor.Tests.Helpers;

/// <summary>
/// bUnit IComponentFactory that swaps a real FDW provider for a <see cref="ProviderStub{TContext}"/>
/// seeded with the supplied context. Use one per provider type when rendering a hosted page so the
/// page's markup renders against a deterministic context with no HTTP.
/// </summary>
public sealed class ProviderStubFactory<TActual, TContext> : IComponentFactory
    where TActual : IComponent
    where TContext : new()
{
    private readonly TContext? _seed;

    public ProviderStubFactory(TContext? seed = default) => _seed = seed;

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
