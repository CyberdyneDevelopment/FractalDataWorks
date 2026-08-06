namespace Fdw.UI.Components.Blazor.Tests.ObsInfra;

/// <summary>
/// Non-generic static store for pending context objects keyed by context type. Separating static
/// state from the generic stub avoids CA1000. Used by <see cref="ProviderStub{TContext}"/> so a
/// hosted FDW page can render against a seeded context without standing up the real provider.
/// </summary>
internal static class ProviderStubState
{
    private static readonly Dictionary<Type, object?> Pending = [];

    internal static void Set<TContext>(TContext? value) =>
        Pending[typeof(TContext)] = value;

    internal static TContext? Take<TContext>()
        where TContext : new()
    {
        if (Pending.TryGetValue(typeof(TContext), out var value))
        {
            Pending.Remove(typeof(TContext));
            return (TContext?)value;
        }

        return default;
    }
}
