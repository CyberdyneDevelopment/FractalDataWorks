using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Data.MsSql;

/// <summary>
/// Reads a T-SQL script (FDW-5457): a parse by ScriptDom, the parser SqlMcp is built on, and a walk of the parse tree for the tables the script
/// reads, writes and creates.
/// </summary>
/// <remarks>
/// A table is named in the catalogue's terms: its schema is the path and its name is the container. What a script holds for itself is not
/// named: a temporary table (<c>#name</c>), a table variable, and a common table expression. A name with a database or server part is in
/// another database, so it is listed as outside the store. Nothing here matches text: every name comes from the parse tree.
/// </remarks>
[TypeOption(typeof(ScriptReaders), "MsSql")]
public sealed class MsSqlScriptReader : ScriptReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlScriptReader"/> class.</summary>
    public MsSqlScriptReader() : base("MsSql")
    {
    }

    /// <inheritdoc />
    public override ScriptReading Read(string script, IReadOnlyCollection<string> runTimeNames)
    {
        TSqlFragment fragment;
        IList<ParseError> errors;
        using (var reader = new StringReader(script))
        {
            fragment = new TSql170Parser(initialQuotedIdentifiers: true).Parse(reader, out errors);
        }

        if (errors.Count > 0)
        {
            return new ScriptReading([.. errors.Select(error => new ScriptParseError(error.Line, error.Column, error.Message))], [], [], [], [], []);
        }

        var targets = new MsSqlScriptTargets();
        fragment.Accept(targets);
        var reads = new MsSqlScriptReads(targets.TargetReferences);
        fragment.Accept(reads);

        var names = new MsSqlScriptNames(runTimeNames, targets.CommonTableNames);
        return new ScriptReading(
            [],
            names.Sort(reads.Reads),
            names.Sort(targets.Writes),
            names.Sort(targets.Creates),
            names.OutsideStore,
            names.RunTimeNamed);
    }
}
