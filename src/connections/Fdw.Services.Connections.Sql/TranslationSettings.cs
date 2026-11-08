namespace Fdw.Services.Connections.Sql;

/// <summary>
/// What a connection tells its translators about the statements it will run (FDW-4267).
/// </summary>
/// <param name="MaxParametersBeforeTempTable">
/// The most values a key list carries as parameters. A key list with more values than this goes to a temporary table, or the dialect's equivalent, that the
/// statement joins on instead (see <see cref="KeyListSpillScope"/>). The connection resolves it once: the number its configuration states, or the default
/// declared on its connection type's service type option.
/// </param>
public sealed record TranslationSettings(int MaxParametersBeforeTempTable)
{
    /// <summary>Resolves the setting of a connection: the number its configuration states, or else the default its connection type declares.</summary>
    /// <param name="configured">The number the connection's configuration states, or null when it states none.</param>
    /// <param name="declaredDefault">The default declared on the connection type's service type option; the factory checks the kind declared one before it resolves.</param>
    /// <returns>The connection's setting.</returns>
    public static TranslationSettings Resolve(int? configured, int declaredDefault)
        => configured is { } own ? new TranslationSettings(own) : new TranslationSettings(declaredDefault);
}
