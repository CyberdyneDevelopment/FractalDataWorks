using System;
using Fdw.Collections;
using Fdw.Protocols.Tds;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// Base class for the built-in scalar functions: checks the argument count, then evaluates.
/// </summary>
public abstract class SqlScalarFunctionBase : TypeOptionBase<SqlScalarFunctionBase>, ISqlScalarFunction
{
    private readonly int _fewest;
    private readonly int _most;

    /// <summary>Initializes a new instance of the <see cref="SqlScalarFunctionBase"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    /// <param name="sqlName">The function's T-SQL name.</param>
    /// <param name="fewestArguments">The fewest arguments it takes.</param>
    /// <param name="mostArguments">The most arguments it takes.</param>
    protected SqlScalarFunctionBase(string name, string sqlName, int fewestArguments, int mostArguments)
        : base(name)
    {
        SqlName = sqlName;
        _fewest = fewestArguments;
        _most = mostArguments;
    }

    /// <inheritdoc/>
    public string SqlName { get; }

    /// <inheritdoc/>
    public IGenericResult<object?> Evaluate(SqlFunctionCall call)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (call.Arguments.Count < _fewest || call.Arguments.Count > _most)
        {
            return call.Refuse(new SqlServerMessage(174, 1, 15,
                $"The {SqlName.ToLowerInvariant()} function requires {Expected()} argument(s).", call.Line));
        }

        return Run(call);
    }

    /// <summary>Evaluates a call with an argument count this function takes.</summary>
    /// <remarks>The function's own body; a caller evaluates through <see cref="Evaluate"/>, which checks the argument count first.</remarks>
    /// <param name="call">The call.</param>
    /// <returns>The value (null for NULL), or the refusal.</returns>
    public abstract IGenericResult<object?> Run(SqlFunctionCall call);

    private string Expected() => _fewest == _most ? $"{_fewest}" : $"{_fewest} to {_most}";
}
