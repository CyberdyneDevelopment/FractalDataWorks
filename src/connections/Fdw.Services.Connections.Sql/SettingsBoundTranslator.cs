using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// A connection's translator, with the connection's setting bound to it (FDW-4267).
/// </summary>
/// <remarks>
/// A translator is one shared instance and the setting belongs to the connection, so the connection hands out this small wrapper from its
/// <c>GetTranslator</c>: every caller, the connection's own and its unit of work, translates through the same setting, and none can forget to state it. A
/// translator that does not filter by key lists is run as it is.
/// </remarks>
/// <typeparam name="TCommand">The provider's command type.</typeparam>
public sealed class SettingsBoundTranslator<TCommand> : IDataCommandTranslator<TCommand>
{
    private readonly IDataCommandTranslator<TCommand> _inner;
    private readonly TranslationSettings _settings;

    /// <summary>Initializes a new instance of the <see cref="SettingsBoundTranslator{TCommand}"/> class.</summary>
    /// <param name="inner">The shared translator.</param>
    /// <param name="settings">The connection's setting.</param>
    public SettingsBoundTranslator(IDataCommandTranslator<TCommand> inner, TranslationSettings settings)
    {
        _inner = inner;
        _settings = settings;
    }

    /// <summary>Binds a connection's setting to a shared translator.</summary>
    /// <param name="translator">The shared translator.</param>
    /// <param name="settings">The connection's setting. Every SQL connection has one: its factory refuses to build a connection without it.</param>
    /// <returns>The translator, bound to the setting.</returns>
    public static IDataCommandTranslator<TCommand> Bind(IDataCommandTranslator<TCommand> translator, TranslationSettings settings)
        => new SettingsBoundTranslator<TCommand>(translator, settings);

    /// <inheritdoc />
    public int Id => _inner.Id;

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string Category => _inner.Category;

    /// <inheritdoc />
    public string DomainName => _inner.DomainName;

    /// <inheritdoc />
    public Task<IGenericResult<TCommand>> Translate(IDataCommand command, IStorageContainer container, CancellationToken cancellationToken = default)
        => _inner is SqlDataCommandTranslatorBase<TCommand> sql
            ? sql.Translate(command, container, _settings, cancellationToken)
            : _inner.Translate(command, container, cancellationToken);
}
