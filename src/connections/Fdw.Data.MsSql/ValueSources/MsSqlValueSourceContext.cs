using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>
/// What a value-source renderer needs to turn a value source into a T-SQL expression: the command to add
/// parameters to, the dialect, and the alias that qualifies the source rows' columns.
/// </summary>
public sealed class MsSqlValueSourceContext
{
    private int _counter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlValueSourceContext"/> class.
    /// </summary>
    /// <param name="command">The command parameters are added to.</param>
    /// <param name="dialect">The dialect that quotes identifiers.</param>
    /// <param name="sourceAlias">The alias that qualifies the source rows' columns in the statement.</param>
    public MsSqlValueSourceContext(SqlCommand command, ISqlDialect dialect, string sourceAlias)
    {
        Command = command;
        Dialect = dialect;
        SourceAlias = sourceAlias;
    }

    /// <summary>Gets the command parameters are added to.</summary>
    public SqlCommand Command { get; }

    /// <summary>Gets the dialect that quotes identifiers.</summary>
    public ISqlDialect Dialect { get; }

    /// <summary>Gets the alias that qualifies the source rows' columns in the statement.</summary>
    public string SourceAlias { get; }

    /// <summary>Returns the next unused parameter name (with its <c>@</c>) built from <paramref name="stem"/>.</summary>
    /// <param name="stem">The readable part of the name.</param>
    public string NextParameterName(string stem)
    {
        _counter++;
        return $"@fdw_{stem}{_counter}";
    }
}
