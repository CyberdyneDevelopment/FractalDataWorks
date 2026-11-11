using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// The setting of the statement being translated, and the key lists that went to a table while it was (FDW-4267).
/// </summary>
/// <remarks>
/// <para>
/// A translator is one shared instance, and the setting belongs to the connection that asked for the translation, so it cannot live on the translator. The
/// scope carries it for the duration of one translation, on the asynchronous flow of that call and nowhere else: the where-clause builder, which every
/// translator that filters reaches from inside its own Translate, reads it from here. A translation always runs in a scope: the connection binds its setting to
/// its translators (<see cref="SettingsBoundTranslator{TCommand}"/>), so a translator that reads the scope outside one was called by something that skipped the
/// connection, and it stops instead of choosing a setting.
/// </para>
/// <para>
/// Each list that goes to a table adds the statements the dialect needs before and after the main statement; the translator base wraps the finished command
/// with them.
/// </para>
/// </remarks>
public sealed class KeyListSpillScope : IDisposable
{
    private static readonly AsyncLocal<KeyListSpillScope?> Active = new();

    private readonly KeyListSpillScope? _outer;
    private readonly List<string> _before = [];
    private readonly List<string> _after = [];

    private KeyListSpillScope(TranslationSettings settings, string? sourceTable)
    {
        Settings = settings;
        SourceTable = sourceTable;
        _outer = Active.Value;
        Active.Value = this;
    }

    /// <summary>Gets the scope of the translation being made on this flow, or null when none is.</summary>
    public static KeyListSpillScope? Current => Active.Value;

    /// <summary>Gets the scope of the translation being made on this flow; reading it outside one is a defect in the caller, and stops there.</summary>
    public static KeyListSpillScope Required
        => Active.Value
           ?? throw new InvalidOperationException(
               "A statement is being translated outside a connection's scope: translate through the connection's translator, which binds the connection's MaxParametersBeforeTempTable.");

    /// <summary>Gets the most parameters a statement being translated carries as values: the setting of the translation in progress.</summary>
    public static int ParameterBudget => Required.Settings.MaxParametersBeforeTempTable;

    /// <summary>Gets the setting of the connection that asked for the translation.</summary>
    public TranslationSettings Settings { get; }

    /// <summary>Gets the quoted, qualified table the statement filters, or null when the container is not a database table.</summary>
    public string? SourceTable { get; }

    /// <summary>Gets the statements to run before the main statement, in the order the lists were met.</summary>
    public IReadOnlyList<string> Before => _before;

    /// <summary>Gets the statements to run after the main statement, in the order the lists were met.</summary>
    public IReadOnlyList<string> After => _after;

    /// <summary>Gets a value indicating whether any list went to a table that needs statements around the main one.</summary>
    public bool HasStatements => _before.Count > 0 || _after.Count > 0;

    /// <summary>Begins the scope of one translation.</summary>
    /// <param name="settings">The setting of the connection that asked for it.</param>
    /// <param name="sourceTable">The quoted, qualified table the statement filters, or null when the container is not a database table.</param>
    /// <returns>The scope; dispose it when the translation is done.</returns>
    public static KeyListSpillScope Begin(TranslationSettings settings, string? sourceTable)
    {
        return new KeyListSpillScope(settings, sourceTable);
    }

    /// <summary>Records the statements of a list that went to a table.</summary>
    /// <param name="form">What the dialect gave for the list.</param>
    public void Add(KeyListSpillForm form)
    {
        if (form.Before.Length > 0)
            _before.Add(form.Before);
        if (form.After.Length > 0)
            _after.Add(form.After);
    }

    /// <inheritdoc />
    public void Dispose() => Active.Value = _outer;
}
