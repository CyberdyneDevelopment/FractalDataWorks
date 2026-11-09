using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.UI.Themes.Configuration;

namespace Fdw.UI.Themes;

/// <summary>Supplies the configured Theme members, and the default-theme and removal rules that only themes have.</summary>
public interface IThemeConfigurationProvider
    : IDomainConfigurationProvider<IThemeImplementationConfiguration>
{
    /// <summary>Reads the theme marked as the default.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The default theme; a failure when the read fails, and a not-found failure when no theme is marked.</returns>
    Task<IGenericResult<IThemeImplementationConfiguration>> GetDefault(CancellationToken ct = default);

    /// <summary>Makes the named theme the default and the one that was the default an ordinary theme.</summary>
    /// <param name="name">The theme to make the default.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success; a not-found failure when no theme carries the name; or the read or write failure.</returns>
    Task<IGenericResult> SetDefault(string name, CancellationToken ct = default);

    /// <summary>Removes the named theme unless it is the default or a built-in theme.</summary>
    /// <param name="name">The theme to remove.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success; a not-found failure; a conflict failure naming why the theme stays; or the read or delete failure.</returns>
    Task<IGenericResult> Remove(string name, CancellationToken ct = default);
}
