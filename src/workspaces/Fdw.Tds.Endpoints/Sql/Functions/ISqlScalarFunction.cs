using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// A T-SQL built-in scalar function this endpoint evaluates.
/// </summary>
public interface ISqlScalarFunction : ITypeOption<int, ISqlScalarFunction>
{
    /// <summary>Gets the function's T-SQL name, such as DB_NAME.</summary>
    string SqlName { get; }

    /// <summary>Evaluates one call.</summary>
    /// <param name="call">The call.</param>
    /// <returns>The value (null for NULL), or the refusal.</returns>
    IGenericResult<object?> Evaluate(SqlFunctionCall call);
}
