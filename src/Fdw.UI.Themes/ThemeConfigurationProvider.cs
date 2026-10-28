using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.UI.Themes.Commands;
using Fdw.UI.Themes.Configuration;
using Fdw.UI.Themes.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Themes;

/// <summary>Supplies the Theme configuration.</summary>
public sealed class ThemeConfigurationProvider
    : DomainConfigurationProviderBase<IThemeImplementationConfiguration>,
      IThemeConfigurationProvider
{
    private readonly ILogger<ThemeConfigurationProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="ThemeConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public ThemeConfigurationProvider(
        ILogger<ThemeConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _logger = logger ?? NullLogger<ThemeConfigurationProvider>.Instance;
    }

    /// <summary>Reads the theme marked as the default.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The default theme; a failure when the read fails, and a not-found failure when no theme is marked.</returns>
    public async Task<IGenericResult<IThemeImplementationConfiguration>> GetDefault(CancellationToken ct = default)
    {
        var all = await Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess) return all.ToNewResult<IThemeImplementationConfiguration>();

        return all.Value?.FirstOrDefault(theme => theme.IsDefault) is { } found
            ? GenericResult<IThemeImplementationConfiguration>.Success(found)
            : GenericResult<IThemeImplementationConfiguration>.Failure(
                ThemeConfigurationProviderLog.NoDefaultTheme(_logger));
    }

    /// <summary>Makes the named theme the default and the one that was the default an ordinary theme.</summary>
    /// <remarks>
    /// One unit of work holds the two records that change. The old default is cleared first: the store
    /// allows one current default row, so marking the new one while the old still is refuses the write.
    /// </remarks>
    /// <param name="name">The theme to make the default.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success; a not-found failure when no theme carries the name; or the read or write failure.</returns>
    public async Task<IGenericResult> SetDefault(string name, CancellationToken ct = default)
    {
        var target = await Get(name, ct).ConfigureAwait(false);
        if (!target.IsSuccess) return target;
        if (target.Value is not { } chosen)
            return GenericResult.Failure(ThemeConfigurationProviderLog.ThemeNotFound(_logger, name));

        var all = await Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess) return all;

        var previous = all.Value?.FirstOrDefault(theme => theme.IsDefault && theme.Id != chosen.Id);
        if (previous is null && chosen.IsDefault)
        {
            ThemeConfigurationProviderLog.DefaultThemeUnchanged(_logger, chosen.Name);
            return GenericResult.Success();
        }

        var moved = await SaveDefaultMove(previous, chosen, ct).ConfigureAwait(false);
        if (moved.IsFailure) return moved;

        if (previous is null)
            ThemeConfigurationProviderLog.DefaultThemeSet(_logger, chosen.Name);
        else
            ThemeConfigurationProviderLog.DefaultThemeMoved(_logger, chosen.Name, previous.Name);
        return moved;
    }

    /// <summary>Removes the named theme unless it is the default or a built-in theme.</summary>
    /// <param name="name">The theme to remove.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success; a not-found failure; a conflict failure naming why the theme stays; or the read or delete failure.</returns>
    public async Task<IGenericResult> Remove(string name, CancellationToken ct = default)
    {
        var found = await Get(name, ct).ConfigureAwait(false);
        if (!found.IsSuccess) return found;
        if (found.Value is not { } theme)
            return GenericResult.Failure(ThemeConfigurationProviderLog.ThemeNotFound(_logger, name));
        if (theme.IsDefault)
            return GenericResult.Failure(ThemeConfigurationProviderLog.CannotRemoveDefaultTheme(_logger, theme.Name));
        if (theme.IsBuiltIn)
            return GenericResult.Failure(ThemeConfigurationProviderLog.CannotRemoveBuiltInTheme(_logger, theme.Name));

        return await Delete(theme.Name, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> SaveDefaultMove(
        IThemeImplementationConfiguration? previous, IThemeImplementationConfiguration chosen, CancellationToken ct)
    {
        var scope = await BeginWriteScope(ct).ConfigureAwait(false);
        if (scope.Value is not { } opened) return scope;

        await using var unit = opened;

        if (previous is not null)
        {
            previous.IsDefault = false;
            var cleared = await Save(previous, previous.Domain, previous.Implementation, previous.Name, unit, ct).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                await unit.Rollback(ct).ConfigureAwait(false);
                return cleared;
            }
        }

        chosen.IsDefault = true;
        var marked = await Save(chosen, chosen.Domain, chosen.Implementation, chosen.Name, unit, ct).ConfigureAwait(false);
        if (marked.IsFailure)
        {
            await unit.Rollback(ct).ConfigureAwait(false);
            return marked;
        }

        return await unit.Commit(ct).ConfigureAwait(false);
    }
}
