using System;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// DI-based screen factory.
/// </summary>
public sealed class ScreenFactory : IScreenFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenFactory"/> class.
    /// </summary>
    public ScreenFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public TScreen Create<TScreen>() where TScreen : IScreen
    {
        return _serviceProvider.GetRequiredService<TScreen>();
    }

    /// <inheritdoc />
    public TScreen Create<TScreen>(params object[] args) where TScreen : IScreen
    {
        return ActivatorUtilities.CreateInstance<TScreen>(_serviceProvider, args);
    }
}
