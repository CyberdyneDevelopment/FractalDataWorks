using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Reads the script of a SQL step (FDW-5457) with the reader of its data store's kind: a real parse in the store's language, never a match on
/// the text.
/// </summary>
/// <remarks>
/// The kind is the data store's own (the name of its data store type), so no connection type is named here: <see cref="ScriptReaders"/> holds a
/// reader for each kind a referenced package reads. Each <c>$(Name)</c> token is replaced first by an identifier that stands for the name the run
/// gives (<see cref="ScriptTokens.Mark"/>), so the parse sees a name where the run will put one, and the reader lists those names apart.
/// </remarks>
public static class GraphScriptReading
{
    /// <summary>Reads the script of a SQL step on the data store it runs on.</summary>
    /// <param name="script">The SQL step.</param>
    /// <param name="store">The data store it runs on.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>What the script names; or why it cannot be read: no reader for the store's kind, or the script does not parse.</returns>
    public static IGenericResult<ScriptReading> Read(GraphScript script, IDataStoreImplementationConfiguration store, ILogger logger)
    {
        var reader = ScriptReaders.ByName(store.Implementation);
        if (ReferenceEquals(reader, ScriptReaders.NotFound))
        {
            return GenericResult<ScriptReading>.Failure(GraphScriptLog.ScriptLanguageNotRead(logger, script.Component, script.DataStore, store.Implementation));
        }

        var marked = ScriptTokens.Mark(script.Script);
        var reading = reader.Read(marked.Text, marked.Markers);
        return reading.Parsed
            ? GenericResult<ScriptReading>.Success(reading)
            : GenericResult<ScriptReading>.Failure(GraphScriptLog.ScriptNotParsed(
                logger, script.Component, script.DataStore, string.Join("; ", reading.Errors.Select(error => string.Create(CultureInfo.InvariantCulture, $"line {error.Line}, column {error.Column}: {error.Text}")))));
    }
}
