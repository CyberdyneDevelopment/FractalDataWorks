using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>The scopes of a host that registers no build contribution, for the tests of a runner whose graphs need none (FDW-6240).</summary>
internal static class NoContributions
{
    /// <summary>Gets a scope factory over a container with nothing registered.</summary>
    /// <returns>The factory.</returns>
    public static IServiceScopeFactory Scopes() =>
        new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
}
