namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Factory for creating screens.
/// </summary>
public interface IScreenFactory
{
    /// <summary>
    /// Creates a screen of the specified type.
    /// </summary>
    /// <typeparam name="TScreen">The type of screen to create.</typeparam>
    /// <returns>The created screen.</returns>
    TScreen Create<TScreen>() where TScreen : IScreen;

    /// <summary>
    /// Creates a screen of the specified type with constructor arguments.
    /// </summary>
    /// <typeparam name="TScreen">The type of screen to create.</typeparam>
    /// <param name="args">Constructor arguments.</param>
    /// <returns>The created screen.</returns>
    TScreen Create<TScreen>(params object[] args) where TScreen : IScreen;
}
